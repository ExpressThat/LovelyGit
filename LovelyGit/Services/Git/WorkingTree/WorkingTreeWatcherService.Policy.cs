namespace ExpressThat.LovelyGit.Services.Git.WorkingTree;

internal sealed partial class WorkingTreeWatcherService
{
    internal readonly record struct WorkTreeWatchRoot(string Path, bool IncludeSubdirectories);

    internal static IReadOnlyList<WorkTreeWatchRoot> GetWorkTreeWatchRoots(string workTreeDirectory)
    {
        if (string.IsNullOrWhiteSpace(workTreeDirectory) || !Directory.Exists(workTreeDirectory))
        {
            return [];
        }

        return [new WorkTreeWatchRoot(workTreeDirectory, IncludeSubdirectories: true)];
    }

    private static bool ShouldWatchAsWorkTreeRoot(string directory)
    {
        var name = Path.GetFileName(directory);
        if (name.Equals(".git", StringComparison.Ordinal))
        {
            return false;
        }

        try
        {
            var attributes = File.GetAttributes(directory);
            return (attributes & FileAttributes.ReparsePoint) == 0;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return false;
        }
    }
}
