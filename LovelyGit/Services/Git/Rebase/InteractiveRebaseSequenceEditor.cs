namespace ExpressThat.LovelyGit.Services.Git.Rebase;

internal static class InteractiveRebaseSequenceEditor
{
    public static string CreateCommand(string todoSourcePath) =>
        $"cp -- {GitShellArgument.Quote(todoSourcePath)}";
}
