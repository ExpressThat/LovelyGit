using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser;

namespace ExpressThat.LovelyGit.Services.Git.WorkingTree;

internal sealed partial class WorkingTreeWatcherService
{
    private bool IsRelevantGitMetadataPath(string path)
    {
        var gitDirectory = GetActiveGitDirectory();
        if (string.IsNullOrEmpty(gitDirectory))
        {
            return false;
        }

        var relativePath = Path.GetRelativePath(gitDirectory, path).Replace('\\', '/');
        return relativePath.Equals("index", StringComparison.Ordinal)
            || relativePath.Equals("index.lock", StringComparison.Ordinal)
            || relativePath.Equals("HEAD", StringComparison.Ordinal)
            || relativePath.Equals("packed-refs", StringComparison.Ordinal)
            || relativePath.StartsWith("refs/", StringComparison.Ordinal);
    }

    private bool IsCommitGraphMetadataPath(string path)
    {
        var gitDirectory = GetActiveGitDirectory();
        if (string.IsNullOrEmpty(gitDirectory))
        {
            return false;
        }

        var relativePath = Path.GetRelativePath(gitDirectory, path).Replace('\\', '/');
        return relativePath.Equals("HEAD", StringComparison.Ordinal)
            || relativePath.Equals("packed-refs", StringComparison.Ordinal)
            || relativePath.StartsWith("refs/", StringComparison.Ordinal);
    }

    private string? GetActiveGitDirectory()
    {
        lock (_lock)
        {
            return _activeGitDirectory;
        }
    }

    private static bool IsIgnoredInternalPath(string path) =>
        path.Contains(
            $"{Path.DirectorySeparatorChar}.git{Path.DirectorySeparatorChar}objects{Path.DirectorySeparatorChar}",
            StringComparison.Ordinal) ||
        path.Contains(
            $"{Path.DirectorySeparatorChar}.git{Path.DirectorySeparatorChar}logs{Path.DirectorySeparatorChar}",
            StringComparison.Ordinal);

    private void OnWatcherError(object sender, ErrorEventArgs eventArgs) => QueueInvalidation();

    private bool IsIgnoreRulePath(string path)
    {
        var gitDirectory = GetActiveGitDirectory();
        return Path.GetFileName(path).Equals(".gitignore", StringComparison.Ordinal)
            || (!string.IsNullOrEmpty(gitDirectory)
                && string.Equals(
                    path,
                    Path.Combine(gitDirectory, "info", "exclude"),
                    StringComparison.OrdinalIgnoreCase));
    }

    private bool IsIgnoredWorkTreePath(string path)
    {
        string? workTreeDirectory;
        GitIgnoreMatcher? matcher;
        lock (_lock)
        {
            workTreeDirectory = _activeWorkTreeDirectory;
            matcher = _ignoreMatcher;
        }

        if (string.IsNullOrEmpty(workTreeDirectory) || matcher == null)
        {
            return false;
        }

        var relativePath = Path.GetRelativePath(workTreeDirectory, path).Replace('\\', '/');
        if (relativePath.StartsWith("../", StringComparison.Ordinal) ||
            relativePath.Equals("..", StringComparison.Ordinal))
        {
            return false;
        }

        if (relativePath.Equals(".git", StringComparison.Ordinal) ||
            relativePath.StartsWith(".git/", StringComparison.Ordinal))
        {
            return true;
        }

        return matcher.IsIgnored(relativePath, Directory.Exists(path));
    }

    private async Task RefreshIgnoreMatcherAsync()
    {
        string? gitDirectory;
        string? workTreeDirectory;
        lock (_lock)
        {
            gitDirectory = _activeGitDirectory;
            workTreeDirectory = _activeWorkTreeDirectory;
        }

        if (string.IsNullOrEmpty(gitDirectory) || string.IsNullOrEmpty(workTreeDirectory))
        {
            return;
        }

        var matcher = await GitIgnoreMatcher
            .LoadAsync(workTreeDirectory, gitDirectory, CancellationToken.None)
            .ConfigureAwait(false);
        lock (_lock)
        {
            if (string.Equals(_activeGitDirectory, gitDirectory, StringComparison.Ordinal) &&
                string.Equals(_activeWorkTreeDirectory, workTreeDirectory, StringComparison.Ordinal))
            {
                _ignoreMatcher = matcher;
            }
        }
    }
}
