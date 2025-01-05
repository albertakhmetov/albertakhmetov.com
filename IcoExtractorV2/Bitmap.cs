namespace IcoExtractor;

using System.Buffers;

class Bitmap
{
    private const int FILE_HEADER_SIZE = 0x0e;
    private const int FILE_HEADER_FILE_SIZE = 0x02;
    private const int FILE_HEADER_PIXELS = 0x0a;

    private const int BITMAP_INFO_HEADER_SIZE = 40;
    private const int BITMAP_V5_HEADER_SIZE = 124;

    private const int BITMAP_HEADER_WIDTH = 0x04;
    private const int BITMAP_HEADER_HEIGHT = 0x08;
    private const int BITMAP_HEADER_COLOR_PANES = 0x0c;
    private const int BITMAP_HEADER_BIT_COUNT = 0x0e;
    private const int BITMAP_HEADER_COMPRESSION = 0x10;
    private const int BITMAP_HEADER_IMAGE_SIZE = 0x14;
    private const int BITMAP_HEADER_COLORS = 0x20;
    private const int BITMAP_HEADER_RED_MASK = 0x28;
    private const int BITMAP_HEADER_GREEN_MASK = 0x2c;
    private const int BITMAP_HEADER_BLUE_MASK = 0x30;
    private const int BITMAP_HEADER_ALPHA_MASK = 0x34;

    private uint[] pixels;
    private bool[] bitmask;

    public Bitmap(Span<byte> buffer)
    {
        // Parse the bitmap parameters
        Height = BitConverter.ToInt32(buffer[BITMAP_HEADER_HEIGHT..]) / 2;
        Width = BitConverter.ToInt32(buffer[BITMAP_HEADER_WIDTH..]);
        BitCount = BitConverter.ToInt16(buffer[BITMAP_HEADER_BIT_COUNT..]);

        // Check the color count
        var colorCount = BitConverter.ToInt32(buffer[BITMAP_HEADER_COLORS..]);
        if (colorCount == 0)
        {
            colorCount = BitCount <= 8 ? (1 << BitCount) : 0;
        }

        // color the table length == 0 for non-indexed bitmaps
        var colorTableLength = colorCount * sizeof(uint);
        var colorTable = colorTableLength == 0
            ? []
            : ParseColorTable(buffer.Slice(BITMAP_INFO_HEADER_SIZE, colorTableLength));

        pixels = BitCount <= 8
            ? ParseIndexedPixels(Width, Height, BitCount, colorTable, buffer[(BITMAP_INFO_HEADER_SIZE + colorTableLength)..])
            : ParsePixels(Width, Height, BitCount == 32, buffer[BITMAP_INFO_HEADER_SIZE..]);

        bitmask = ParseIndexedPixels(Width, Height, 1, [true, false], buffer[^(GetStride(Width, 1) * Height)..]);
    }

    public int Width { get; }

    public int Height { get; }

    public int BitCount { get; }

    /// <summary>
    /// Saves the 32-bit bitmap to the stream
    /// </summary>
    /// <param name="stream">The output stream</param>
    public void Save(Stream stream)
    {
        const int BI_BITFIELDS = 0x0003;

        var pixelBufferLength = Width * Height * sizeof(uint);
        var imageLength = FILE_HEADER_SIZE + BITMAP_V5_HEADER_SIZE + pixelBufferLength;

        // rent the array for the file
        var buffer = ArrayPool<byte>.Shared.Rent(imageLength);
        try
        {
            // bitmap file header
            var headerBuffer = buffer.AsSpan(0, FILE_HEADER_SIZE);
            headerBuffer.Clear();

            // write the file signature
            headerBuffer[0] = (byte)'B';
            headerBuffer[1] = (byte)'M';

            // write the full file size: add the header size and subtract the bitmask length
            BitConverter.TryWriteBytes(
                headerBuffer[FILE_HEADER_FILE_SIZE..],
                imageLength);

            // write the pixel array position: file header + bitmap header
            BitConverter.TryWriteBytes(
                headerBuffer[FILE_HEADER_PIXELS..],
                FILE_HEADER_SIZE + BITMAP_V5_HEADER_SIZE);

            // bitmap header
            var bitmapHeaderBuffer = buffer.AsSpan(FILE_HEADER_SIZE, BITMAP_V5_HEADER_SIZE);
            bitmapHeaderBuffer.Clear();

            // write the image parameters
            BitConverter.TryWriteBytes(bitmapHeaderBuffer, BITMAP_V5_HEADER_SIZE);
            BitConverter.TryWriteBytes(bitmapHeaderBuffer[BITMAP_HEADER_WIDTH..], Width);
            BitConverter.TryWriteBytes(bitmapHeaderBuffer[BITMAP_HEADER_HEIGHT..], Height);
            BitConverter.TryWriteBytes(bitmapHeaderBuffer[BITMAP_HEADER_COLOR_PANES..], 1);
            BitConverter.TryWriteBytes(bitmapHeaderBuffer[BITMAP_HEADER_BIT_COUNT..], 32);

            // write the bitfields masks
            BitConverter.TryWriteBytes(bitmapHeaderBuffer[BITMAP_HEADER_COMPRESSION..], BI_BITFIELDS);
            BitConverter.TryWriteBytes(bitmapHeaderBuffer[BITMAP_HEADER_RED_MASK..], (uint)0x00ff0000);
            BitConverter.TryWriteBytes(bitmapHeaderBuffer[BITMAP_HEADER_GREEN_MASK..], (uint)0x00ff00);
            BitConverter.TryWriteBytes(bitmapHeaderBuffer[BITMAP_HEADER_BLUE_MASK..], (uint)0x000000ff);
            BitConverter.TryWriteBytes(bitmapHeaderBuffer[BITMAP_HEADER_ALPHA_MASK..], (uint)0xff000000);

            // write the pixel array
            WritePixels(
                Width,
                Height,
                pixels,
                BitCount == 32 ? [] : bitmask,
                buffer.AsSpan(FILE_HEADER_SIZE + BITMAP_V5_HEADER_SIZE, pixelBufferLength));

            // save the image into the stream
            stream.Write(buffer.AsSpan(0, imageLength));
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    /// <summary>
    /// Writes the pixel into the buffer
    /// </summary>
    /// <param name="width">The image width</param>
    /// <param name="height">The image height</param>
    /// <param name="pixels">The array of pixels</param>
    /// <param name="bitmask">The bitmask (or empty array if bit count = 32)</param>
    /// <param name="buffer">The source buffer</param>
    private static void WritePixels(int width, int height, uint[] pixels, bool[] bitmask, Span<byte> buffer)
    {
        var hasAlpha = bitmask.Length == 0;

        buffer.Clear();

        var pos = 0;
        var mask = (1u << 8) - 1;

        for (var y = height; y > 0; y--)
        {
            for (var x = 0; x < width; x++)
            {
                // check the bitmask for the transparency for non-32 bit bitmaps
                var color = hasAlpha || bitmask[width * (y - 1) + x]
                    ? pixels[width * (y - 1) + x]
                    : 0x00000000;

                buffer[pos++] = (byte)(color & mask);
                buffer[pos++] = (byte)((color >> 8) & mask);
                buffer[pos++] = (byte)((color >> 16) & mask);

                // use the native transparency for 32 bit bitmaps or the bitmask value
                buffer[pos++] = hasAlpha
                    ? (byte)((color >> 24) & mask)
                    : bitmask[width * (y - 1) + x] ? (byte)0xFF : (byte)00;
            }
        }
    }

    /// <summary>
    /// Parses the color table of the indexed bitmap
    /// </summary>
    /// <param name="buffer">The source buffer</param>
    /// <returns>The pixel array</returns>
    private static uint[] ParseColorTable(Span<byte> buffer)
    {
        var colorTableLength = buffer.Length / sizeof(uint);
        var pos = 0;

        var colorTable = new uint[colorTableLength];
        for (var i = 0; i < colorTable.Length; i++)
        {
            var b = buffer[pos++];
            var g = buffer[pos++];
            var r = buffer[pos++];
            var a = buffer[pos++];

            colorTable[i] = (uint)
                (0xFFFFFFFF & (a << 24 | r << 16 | g << 8 | b));
        }

        return colorTable;
    }

    /// <summary>
    /// Parses the pixel buffer of the ordinary bitmap
    /// </summary>
    /// <param name="width">The image width</param>
    /// <param name="height">The image height</param>
    /// <param name="hasAlpha">Determines the presence of an alpha channel</param>
    /// <param name="pixelBuffer">The source buffer</param>
    /// <returns>The pixel array</returns>
    private static uint[] ParsePixels(int width, int height, bool hasAlpha, Span<byte> pixelBuffer)
    {
        var pixels = new uint[width * height];
        var pos = 0;

        for (var y = height; y > 0; y--)
        {
            for (var x = 0; x < width; x++)
            {
                var b = pixelBuffer[pos++]; // blue
                var g = pixelBuffer[pos++]; // green
                var r = pixelBuffer[pos++]; // red

                if (hasAlpha)
                {
                    var a = pixelBuffer[pos++]; // alpha

                    pixels[width * (y - 1) + x] = (uint)
                        (0xFFFFFFFF & (a << 24 | r << 16 | g << 8 | b));
                }
                else
                {
                    pixels[width * (y - 1) + x] = (uint)
                        (0xFFFFFFFF & ((r + b + g == 0 ? 0 : 255) << 24 | r << 16 | g << 8 | b));
                }
            }
        }

        return pixels;
    }

    /// <summary>
    /// Parses the pixel buffer of the indexed bitmap
    /// </summary>
    /// <typeparam name="T">The type of the result data</typeparam>
    /// <param name="width">The image width</param>
    /// <param name="height">The image height</param>
    /// <param name="bitCount">The number of bits by the pixel</param>
    /// <param name="valueTable">The directory for indexed values</param>
    /// <param name="pixelBuffer">The source buffer</param>
    /// <returns>The pixel array</returns>
    private static T[] ParseIndexedPixels<T>(int width, int height, int bitCount, IList<T> valueTable, Span<byte> pixelBuffer)
    {
        var rawDataSize = width * height;
        var rawData = ArrayPool<byte>.Shared.Rent(rawDataSize);

        var pixels = new T[rawDataSize];
        try
        {
            ReadBits(width, height, bitCount, pixelBuffer, rawData.AsSpan(0, rawDataSize));

            for (var i = 0; i < pixels.Length; i++)
            {
                pixels[i] = valueTable[rawData[i]];
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(rawData);
        }

        return pixels;
    }

    /// <summary>
    /// Reads data from the buffer bit by bit
    /// </summary>
    /// <param name="width">The image width</param>
    /// <param name="height">The image height</param>
    /// <param name="bitCount">The number of bits by the pixel</param>
    /// <param name="buffer">The source buffer</param>
    /// <param name="data">The extracted data</param>
    private static void ReadBits(int width, int height, int bitCount, Span<byte> buffer, Span<byte> data)
    {
        var pos = 0;

        var x = 0;
        var y = height;

        var paddingBytes = GetPaddingBytes(width, bitCount);

        var b = buffer[pos++];
        var bPos = 0;

        do
        {
            var mask = (1 << bitCount) - 1;
            var result = (b >> (8 - bPos - bitCount)) & mask;

            data[width * (y - 1) + x] = (byte)result;

            if (x < width - 1)
            {
                x++;
            }
            else
            {
                x = 0;
                y--;
                pos += paddingBytes;
                bPos = 8;
            }

            if (bPos + bitCount >= 8 && y > 0)
            {
                b = buffer[pos++];
                bPos = 0;
            }
            else
            {
                bPos += bitCount;
            }
        }
        while (y > 0);
    }

    /// <summary>
    /// Calcultates the bitmap pixel area row size 
    /// </summary>
    /// <param name="width">The image width</param>
    /// <param name="bitCount">The number of bits by the pixel</param>
    /// <returns>The size of the bitmap pixel area row (in bits)</returns>
    private static int GetStride(int width, int bitCount)
    {
        return (((width * bitCount) + 31) & ~31) >> 3;
    }

    /// <summary>
    /// Calculates the padding bytes
    /// </summary>
    /// <param name="width">The image width</param>
    /// <param name="bitCount">The number of bits by the pixel</param>
    /// <returns>The size of empty bytes in the end of the row</returns>
    private static int GetPaddingBytes(int width, int bitCount)
    {
        return GetStride(width, bitCount) - Convert.ToInt32(Math.Ceiling(width * bitCount / 8.0));
    }
}