namespace LovelyGit.DiffBenchmarks;

internal static class AsciiText
{
    public static bool IsAscii(string value)
    {
        foreach (var ch in value)
        {
            if (ch > 0x7f)
            {
                return false;
            }
        }

        return true;
    }
}
