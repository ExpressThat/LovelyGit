using System.Text;

namespace ExpressThat.LovelyGit.Services.Git.FileBlame;

internal readonly record struct BlameText(string Content, int LineCount)
{
    private const int BinaryProbeLength = 8_000;
    public const int MaximumBytes = 4 * 1024 * 1024;
    public const int MaximumLines = 50_000;

    public static BlameText Decode(byte[] bytes)
    {
        if (bytes.Length > MaximumBytes)
        {
            throw new InvalidDataException("File blame supports text files up to 4 MB.");
        }

        if (bytes.AsSpan(0, Math.Min(bytes.Length, BinaryProbeLength)).Contains((byte)0))
        {
            throw new InvalidDataException("Binary files cannot be blamed.");
        }

        var lineCount = CountLines(bytes);
        if (lineCount > MaximumLines)
        {
            throw new InvalidDataException("File blame supports up to 50,000 lines.");
        }

        return new BlameText(Encoding.UTF8.GetString(bytes), lineCount);
    }

    private static int CountLines(ReadOnlySpan<byte> bytes)
    {
        if (bytes.IsEmpty)
        {
            return 0;
        }

        var count = 0;
        foreach (var value in bytes)
        {
            if (value == (byte)'\n') count++;
        }

        return bytes[^1] == (byte)'\n' ? count : count + 1;
    }
}
