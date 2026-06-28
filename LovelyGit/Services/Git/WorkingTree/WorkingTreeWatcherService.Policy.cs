namespace ExpressThat.LovelyGit.Services.Git.WorkingTree;

internal sealed partial class WorkingTreeWatcherService
{
    internal static bool ShouldWatchWorkTreeRecursively(string workTreeDirectory)
    {
        return !string.IsNullOrWhiteSpace(workTreeDirectory)
            && Directory.Exists(workTreeDirectory);
    }
}
