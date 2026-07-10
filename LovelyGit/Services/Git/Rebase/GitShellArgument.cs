namespace ExpressThat.LovelyGit.Services.Git.Rebase;

internal static class GitShellArgument
{
    public static string Quote(string value) =>
        $"'{value.Replace("'", "'\"'\"'", StringComparison.Ordinal)}'";
}
