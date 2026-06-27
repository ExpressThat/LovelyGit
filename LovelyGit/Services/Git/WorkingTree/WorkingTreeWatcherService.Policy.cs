namespace ExpressThat.LovelyGit.Services.Git.WorkingTree;

internal sealed partial class WorkingTreeWatcherService
{
    internal static bool ShouldWatchWorkTreeRecursively(string workTreeDirectory)
    {
        if (string.IsNullOrWhiteSpace(workTreeDirectory) || !Directory.Exists(workTreeDirectory))
        {
            return false;
        }

        if (OperatingSystem.IsWindows())
        {
            return true;
        }

        return CountDirectoriesUpToLimit(
            workTreeDirectory,
            MaxRecursiveWorkTreeWatcherDirectories + 1) <= MaxRecursiveWorkTreeWatcherDirectories;
    }

    private static int CountDirectoriesUpToLimit(string rootDirectory, int limit)
    {
        var count = 0;
        var pending = new Stack<string>();
        pending.Push(rootDirectory);

        while (pending.Count > 0)
        {
            var current = pending.Pop();
            foreach (var child in EnumerateDirectories(current))
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

    private static IEnumerable<string> EnumerateDirectories(string directory)
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
}
