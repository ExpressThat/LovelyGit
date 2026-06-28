using ExpressThat.LovelyGit.Services.Data.Models;
using ExpressThat.LovelyGit.Services.Data.Repositorys;
using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser;
using ExpressThat.LovelyGit.Services.Git.WorkingTree.Models;
using ExpressThat.LovelyGit.Services.NativeMessaging;
using System.Buffers;
using System.Diagnostics;

namespace ExpressThat.LovelyGit.Services.Git.WorkingTree;

internal sealed partial class WorkingTreeWatcherService : IDisposable
{
    private void AddWatcher(string path, string filter, bool includeSubdirectories)
    {
        if (!Directory.Exists(path))
        {
            return;
        }

        var watcher = new FileSystemWatcher(path, filter)
        {
            IncludeSubdirectories = includeSubdirectories,
            NotifyFilter = NotifyFilters.FileName
                | NotifyFilters.DirectoryName
                | NotifyFilters.LastWrite
                | NotifyFilters.Size
                | NotifyFilters.CreationTime,
            EnableRaisingEvents = true,
        };
        watcher.Changed += OnFileChanged;
        watcher.Created += OnFileChanged;
        watcher.Deleted += OnFileChanged;
        watcher.Renamed += OnFileChanged;
        watcher.Error += OnWatcherError;
        _watchers.Add(watcher);
    }

    private void OnFileChanged(object sender, FileSystemEventArgs eventArgs)
    {
        if (IsRelevantGitMetadataPath(eventArgs.FullPath))
        {
            QueueInvalidation();
            if (IsCommitGraphMetadataPath(eventArgs.FullPath))
            {
                QueueGraphInvalidation();
            }

            return;
        }

        if (IsIgnoredInternalPath(eventArgs.FullPath))
        {
            return;
        }

        if (IsIgnoreRulePath(eventArgs.FullPath))
        {
            _ = RefreshIgnoreMatcherAsync();
            QueueInvalidation();
            return;
        }

        if (IsIgnoredWorkTreePath(eventArgs.FullPath))
        {
            return;
        }

        QueueInvalidation(CreateObservedChange(eventArgs));
    }

    private WorkingTreeChangedFile? CreateObservedChange(FileSystemEventArgs eventArgs)
    {
        string? workTreeDirectory;
        lock (_lock)
        {
            workTreeDirectory = _activeWorkTreeDirectory;
        }

        if (string.IsNullOrEmpty(workTreeDirectory))
        {
            return null;
        }

        var path = NormalizeObservedPath(workTreeDirectory, eventArgs.FullPath);
        if (path == null || Directory.Exists(eventArgs.FullPath))
        {
            return null;
        }

        var oldPath = eventArgs is RenamedEventArgs renamed
            ? NormalizeObservedPath(workTreeDirectory, renamed.OldFullPath)
            : null;
        var status = ToObservedStatus(eventArgs.ChangeType);
        return new WorkingTreeChangedFile
        {
            Path = path,
            OldPath = oldPath,
            Status = status,
            Group = status == "Added"
                ? WorkingTreeChangeGroup.Untracked
                : WorkingTreeChangeGroup.Unstaged,
        };
    }

    private static string? NormalizeObservedPath(string workTreeDirectory, string path)
    {
        var relativePath = Path.GetRelativePath(workTreeDirectory, path).Replace('\\', '/');
        if (relativePath.Equals("..", StringComparison.Ordinal)
            || relativePath.StartsWith("../", StringComparison.Ordinal)
            || relativePath.Equals(".git", StringComparison.Ordinal)
            || relativePath.StartsWith(".git/", StringComparison.Ordinal))
        {
            return null;
        }

        return relativePath;
    }

    private static string ToObservedStatus(WatcherChangeTypes changeType) =>
        changeType switch
        {
            WatcherChangeTypes.Created or WatcherChangeTypes.Renamed => "Added",
            WatcherChangeTypes.Deleted => "Deleted",
            _ => "Modified",
        };

    private bool IsRelevantGitMetadataPath(string path)
    {
        string? gitDirectory;
        lock (_lock)
        {
            gitDirectory = _activeGitDirectory;
        }

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
        string? gitDirectory;
        lock (_lock)
        {
            gitDirectory = _activeGitDirectory;
        }

        if (string.IsNullOrEmpty(gitDirectory))
        {
            return false;
        }

        var relativePath = Path.GetRelativePath(gitDirectory, path).Replace('\\', '/');
        return relativePath.Equals("HEAD", StringComparison.Ordinal)
            || relativePath.Equals("packed-refs", StringComparison.Ordinal)
            || relativePath.StartsWith("refs/", StringComparison.Ordinal);
    }

    private static bool IsIgnoredInternalPath(string path)
    {
        return path.Contains($"{Path.DirectorySeparatorChar}.git{Path.DirectorySeparatorChar}objects{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
            || path.Contains($"{Path.DirectorySeparatorChar}.git{Path.DirectorySeparatorChar}logs{Path.DirectorySeparatorChar}", StringComparison.Ordinal);
    }

    private void OnWatcherError(object sender, ErrorEventArgs eventArgs)
    {
        QueueInvalidation();
    }

    private bool IsIgnoreRulePath(string path)
    {
        string? gitDirectory;
        lock (_lock)
        {
            gitDirectory = _activeGitDirectory;
        }

        return Path.GetFileName(path).Equals(".gitignore", StringComparison.Ordinal)
            || (!string.IsNullOrEmpty(gitDirectory)
                && string.Equals(path, Path.Combine(gitDirectory, "info", "exclude"), StringComparison.OrdinalIgnoreCase));
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
        if (relativePath.StartsWith("../", StringComparison.Ordinal) || relativePath.Equals("..", StringComparison.Ordinal))
        {
            return false;
        }

        if (relativePath.Equals(".git", StringComparison.Ordinal) || relativePath.StartsWith(".git/", StringComparison.Ordinal))
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
            if (string.Equals(_activeGitDirectory, gitDirectory, StringComparison.Ordinal)
                && string.Equals(_activeWorkTreeDirectory, workTreeDirectory, StringComparison.Ordinal))
            {
                _ignoreMatcher = matcher;
            }
        }
    }

}
