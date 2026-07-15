namespace ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser.Refs;

internal static class GitLooseRefReader
{
    public static bool TryReadObjectId(
        string path,
        GitObjectFormat objectFormat,
        out GitObjectId id)
    {
        Span<byte> buffer = stackalloc byte[128];
        using var handle = File.OpenHandle(
            path,
            FileMode.Open,
            FileAccess.Read,
            FileShare.ReadWrite | FileShare.Delete,
            FileOptions.SequentialScan);
        var length = RandomAccess.Read(handle, buffer, 0);
        Span<byte> overflow = stackalloc byte[1];
        if (length == buffer.Length && RandomAccess.Read(handle, overflow, length) != 0)
        {
            return GitObjectId.TryParse(
                File.ReadAllText(path).AsSpan().Trim(),
                objectFormat,
                out id);
        }

        return GitObjectId.TryParseAscii(
            TrimAsciiWhitespace(buffer[..length]),
            objectFormat,
            out id);
    }

    private static ReadOnlySpan<byte> TrimAsciiWhitespace(ReadOnlySpan<byte> value)
    {
        var start = 0;
        while (start < value.Length && IsAsciiWhitespace(value[start])) start++;
        var end = value.Length;
        while (end > start && IsAsciiWhitespace(value[end - 1])) end--;
        return value[start..end];
    }

    private static bool IsAsciiWhitespace(byte value) =>
        value is (byte)' ' or (byte)'\t' or (byte)'\r' or (byte)'\n' or 0x0b or 0x0c;
}
