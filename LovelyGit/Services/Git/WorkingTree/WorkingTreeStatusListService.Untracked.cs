using ExpressThat.LovelyGit.Services.Git.WorkingTree.Models;

namespace ExpressThat.LovelyGit.Services.Git.WorkingTree;

internal sealed partial class WorkingTreeStatusListService
{
    private static async Task<List<WorkingTreeChangedFile>> FindUntrackedFilesAsync(
        string workTreeDirectory,
        string gitDirectory,
        HashSet<string> rootTrackedFiles,
        HashSet<string> rootTrackedDirectories,
        CancellationToken cancellationToken,
        bool scanTrackedDirectories = true)
    {
        var matcher = await GitIgnoreMatcher
            .LoadAsync(workTreeDirectory, gitDirectory, cancellationToken)
            .ConfigureAwait(false);
        var files = new List<WorkingTreeChangedFile>();
        var pending = new Stack<string>();
        pending.Push(workTreeDirectory);
        var visitedDirectories = 0;

        while (pending.Count > 0
            && files.Count < MaxNativeUntrackedFiles
            && visitedDirectories < MaxNativeUntrackedDirectories)
        {
            cancellationToken.ThrowIfCancellationRequested();
            visitedDirectories++;
            var directory = pending.Pop();
            foreach (var file in SafeEnumerateFiles(directory))
            {
                AddUntrackedFile(workTreeDirectory, rootTrackedFiles, matcher, files, file);
                if (files.Count >= MaxNativeUntrackedFiles)
                {
                    break;
                }
            }

            if (!scanTrackedDirectories && directory == workTreeDirectory)
            {
                continue;
            }

            foreach (var childDirectory in SafeEnumerateDirectories(directory))
            {
                AddPendingDirectory(
                    workTreeDirectory,
                    rootTrackedDirectories,
                    matcher,
                    pending,
                    childDirectory);
            }
        }

        files.Sort((left, right) => string.Compare(left.Path, right.Path, StringComparison.Ordinal));
        return files;
    }

    private static void AddUntrackedFile(
        string workTreeDirectory,
        HashSet<string> rootTrackedFiles,
        GitIgnoreMatcher matcher,
        List<WorkingTreeChangedFile> files,
        string path)
    {
        var relative = NormalizeWorkTreePath(workTreeDirectory, path);
        if (relative.StartsWith(".git/", StringComparison.Ordinal)
            || rootTrackedFiles.Contains(relative)
            || matcher.IsIgnored(relative, isDirectory: false))
        {
            return;
        }

        files.Add(Create(relative, null, "Added", WorkingTreeChangeGroup.Untracked));
    }

    private static void AddPendingDirectory(
        string workTreeDirectory,
        HashSet<string> rootTrackedDirectories,
        GitIgnoreMatcher matcher,
        Stack<string> pending,
        string path)
    {
        var relative = NormalizeWorkTreePath(workTreeDirectory, path);
        if (relative.Equals(".git", StringComparison.Ordinal)
            || relative.StartsWith(".git/", StringComparison.Ordinal)
            || !rootTrackedDirectories.Contains(relative)
            || matcher.IsIgnored(relative, isDirectory: true))
        {
            return;
        }

        pending.Push(path);
    }

    private static string NormalizeWorkTreePath(string workTreeDirectory, string path) =>
        Path.GetRelativePath(workTreeDirectory, path).Replace('\\', '/');

    private static IEnumerable<string> SafeEnumerateFiles(string directory)
    {
        try
        {
            return Directory.EnumerateFiles(directory);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return [];
        }
    }

    private static IEnumerable<string> SafeEnumerateDirectories(string directory)
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
