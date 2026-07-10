namespace ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser.Operations;

internal static class GitRepositoryOperationStateReader
{
    public static GitRepositoryOperationKind? Read(string gitDirectory)
    {
        if (File.Exists(Path.Combine(gitDirectory, "CHERRY_PICK_HEAD")))
        {
            return GitRepositoryOperationKind.CherryPick;
        }

        if (File.Exists(Path.Combine(gitDirectory, "MERGE_HEAD")))
        {
            return GitRepositoryOperationKind.Merge;
        }

        return Directory.Exists(Path.Combine(gitDirectory, "rebase-merge")) ||
            Directory.Exists(Path.Combine(gitDirectory, "rebase-apply"))
                ? GitRepositoryOperationKind.Rebase
                : null;
    }
}
