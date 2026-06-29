namespace LovelyGit.DiffBenchmarks;

internal static class DiffJsonEscapeCharacters
{
    public static readonly char[] Values = Create();

    private static char[] Create()
    {
        var chars = new char[34];
        for (var index = 0; index < 32; index++)
        {
            chars[index] = (char)index;
        }

        chars[32] = '"';
        chars[33] = '\\';
        return chars;
    }
}
