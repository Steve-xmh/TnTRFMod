namespace TnTRFMod.Utils;

public static class StreamUtils
{
    public static byte[] ReadExactly(this Stream stream, int count)
    {
        var buffer = new byte[count];
        var offset = 0;
        while (offset < count)
        {
            var read = stream.Read(buffer, offset, count - offset);
            if (read == 0)
                throw new EndOfStreamException();
            offset += read;
        }

        return buffer;
    }

    public static void ReadExactly(this Stream stream, ref byte[] buffer)
    {
        var offset = 0;
        while (offset < buffer.Length)
        {
            var read = stream.Read(buffer, offset, buffer.Length - offset);
            if (read == 0)
                throw new EndOfStreamException();
            offset += read;
        }
    }
}