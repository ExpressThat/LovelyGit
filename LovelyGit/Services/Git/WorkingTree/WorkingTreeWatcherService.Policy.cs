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

        if (ShouldWatchWorkTreeRecursively(workTreeDirectory))
        {
            return [new WorkTreeWatchRoot(workTreeDirectory, IncludeSubdirectories: true)];
        }

        return
        [
            new WorkTreeWatchRoot(workTreeDirectory, IncludeSubdirectories: false),
            .. EnumerateChildDirectories(workTreeDirectory)
                .Where(ShouldWatchAsWorkTreeRoot)
                .Select(path => new WorkTreeWatchRoot(path, IncludeSubdirectories: true)),
        ];
    }

    private static bool ShouldWatchWorkTreeRecursively(string workTreeDirectory)
    {
        return !string.IsNullOrWhiteSpace(workTreeDirectory)
            && Directory.Exists(workTreeDirectory)
            && CountDirectoriesUpToLimit(workTreeDirectory, MaxRecursiveWorkTreeWatcherDirectories + 1)
                <= MaxRecursiveWorkTreeWatcherDirectories;
    }

    private static int CountDirectoriesUpToLimit(string root, int limit)
    {
        var count = 0;
        var pending = new Stack<string>();
        pending.Push(root);
        while (pending.Count > 0)
        {
            var directory = pending.Pop();
            foreach (var child in EnumerateChildDirectories(directory))
            {
                count++;
                if (count >= limit)
                {
                    return count;
                }

                pending.Push(child);
            }
        }

        return count;
    }

    private static IEnumerable<string> EnumerateChildDirectories(string directory)
    {
        try
        {
            return Directory.EnumerateDirectories(directory);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return [];
        }
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
