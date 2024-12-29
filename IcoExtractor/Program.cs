using System.Buffers;
using System.Drawing;

try
{
    if (args.Length == 0)
    {
        throw new InvalidOperationException("Set a path for the icon file");
    }

    var fileName = args[0];
    var outputDirectory = args[0] + ".frames";

    if (Directory.Exists(outputDirectory))
    {
        throw new InvalidOperationException($"Destination directory already exists ({outputDirectory})");
    }
    else
    {
        Directory.CreateDirectory(outputDirectory);
    }

    using var stream = File.OpenRead(fileName);

    if (stream.Length > int.MaxValue)
    {
        throw new InvalidOperationException("File is too big for the icon file");
    }

    var length = (int)stream.Length;

    var array = ArrayPool<byte>.Shared.Rent(length);
    try
    {
        var buffer = array.AsSpan(0, length);

        stream.ReadExactly(buffer);

        if (BitConverter.ToUInt16(buffer.Slice(0, sizeof(ushort))) != 0 || BitConverter.ToUInt16(buffer.Slice(sizeof(ushort), sizeof(ushort))) != 1)
        {
            throw new InvalidOperationException("File is broken");
        }

        ExtractFrames(buffer, outputDirectory);
    }
    finally
    {
        ArrayPool<byte>.Shared.Return(array);
    }
}
catch (Exception ex)
{
    Console.WriteLine(ex.Message);
}

const int ICON_DIR_SIZE = 6;
const int ICON_DIR_ENTRY_SIZE = 16;

int GetFramesCount(Span<byte> buffer)
{
    return BitConverter.ToUInt16(buffer.Slice(sizeof(ushort) * 2, sizeof(ushort)));
}

Bitmap GetFrame(Span<byte> buffer, int frameId)
{
    var entryBuffer = buffer
        .Slice(ICON_DIR_SIZE)
        .Slice(ICON_DIR_ENTRY_SIZE * frameId, ICON_DIR_ENTRY_SIZE);

    int pos = BitConverter.ToInt32(entryBuffer.Slice(ICON_DIR_ENTRY_SIZE - sizeof(uint), sizeof(uint)));
    int length = BitConverter.ToInt32(entryBuffer.Slice(ICON_DIR_ENTRY_SIZE - sizeof(uint) * 2, sizeof(uint)));

    using var handle = Windows.Win32.PInvoke.CreateIconFromResourceEx(
        buffer.Slice(pos, length),
        new Windows.Win32.Foundation.BOOL(true),
        0x00030000,
        0,
        0,
        0);

    using var icon = Icon.FromHandle(handle.DangerousGetHandle());

    return icon.ToBitmap();
}

void ExtractFrames(Span<byte> buffer, string outputDirectory)
{
    var framesCount = GetFramesCount(buffer);

    for (var i = 0; i < framesCount; i++)
    {
        using var bitmap = GetFrame(buffer, i);
        bitmap.Save(Path.Combine(outputDirectory, $"{i}.png"), System.Drawing.Imaging.ImageFormat.Png);
    }
}