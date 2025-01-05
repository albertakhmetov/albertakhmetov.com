using IcoExtractor;

const int ICON_DIR_SIZE = 6;
const int ICON_DIR_ENTRY_SIZE = 16;

long PngSignature = BitConverter.ToInt64(new byte[] {
        (byte)0x89,
        (byte)0x50,
        (byte)0x4e,
        (byte)0x47,
        (byte)0x0d,
        (byte)0x0a,
        (byte)0x1a,
        (byte)0x0a
    });

var fileName = args[0];
var outputDirectory = fileName + ".frames";

if (Directory.Exists(outputDirectory))
{
   throw new InvalidOperationException($"Destination directory already exists ({outputDirectory})");
}
else
{
    Directory.CreateDirectory(outputDirectory);
}

using var file = File.OpenRead(fileName);

// load the ICO file into the buffer
var buffer = new byte[file.Length];
file.ReadExactly(buffer);

// read the frame count from the header
var frameCount = BitConverter.ToInt16(buffer.AsSpan(4));

// load all the directory entities
var directoryBuffer = buffer.AsSpan(ICON_DIR_SIZE, ICON_DIR_ENTRY_SIZE * frameCount);

for (var i = 0; i < frameCount; i++)
{
    // this buffer contains the directory entity with i-index
    var entryBuffer = directoryBuffer.Slice(ICON_DIR_ENTRY_SIZE * i, ICON_DIR_ENTRY_SIZE);

    // extract the position and length of the content
    int pos = BitConverter.ToInt32(entryBuffer[(ICON_DIR_ENTRY_SIZE - sizeof(uint))..]);
    int length = BitConverter.ToInt32(entryBuffer[(ICON_DIR_ENTRY_SIZE - sizeof(uint) * 2)..]);

    // this buffer contains the image content
    var contentBuffer = buffer.AsSpan(pos, length);

    var outputStream = default(Stream);
    try
    {
        // check the PNG signature
        if (IsPng(contentBuffer))
        {
            // save the PNG file
            outputStream = File.Create(Path.Combine(outputDirectory, $"{i}.png"));
            outputStream.Write(contentBuffer);
        }
        else
        {
            outputStream = File.Create(Path.Combine(outputDirectory, $"{i}.bmp"));

            // load bitmap from the ICO file
            var bitmap = new Bitmap(contentBuffer);

            // save the bitmap to the 32-bit bitmap
            // transparency is calculated using the bitmask of the ICO file
            bitmap.Save(outputStream);
        }
    }
    finally
    {
        outputStream?.Dispose();
    }
}

bool IsPng(Span<byte> buffer)
{
    return buffer.Length > sizeof(long)
        && BitConverter.ToInt64(buffer[..sizeof(long)]) == PngSignature;
}