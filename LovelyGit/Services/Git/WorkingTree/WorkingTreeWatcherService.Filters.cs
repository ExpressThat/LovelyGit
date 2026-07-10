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

    private void AddWorkTreeWatchers(IReadOnlyList<WorkTreeWatchRoot> watchRoots)
    {
        foreach (var watchRoot in watchRoots)
        {
            if (_watchedWorkTreePaths.Add(watchRoot.Path))
            {
                AddWatcher(watchRoot.Path, "*", watchRoot.IncludeSubdirectories);
            }
        }
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

        TryAddCreatedWorkTreeDirectoryWatcher(eventArgs.FullPath);
        QueueInvalidation(CreateObservedChange(eventArgs));
    }

    private void TryAddCreatedWorkTreeDirectoryWatcher(string path)
    {
        string? workTreeDirectory;
        lock (_lock)
        {
            workTreeDirectory = _activeWorkTreeDirectory;
        }

        if (string.IsNullOrEmpty(workTreeDirectory)
            || !Directory.Exists(path)
            || Path.GetDirectoryName(path) is not { } parent
            || !string.Equals(parent, workTreeDirectory, StringComparison.OrdinalIgnoreCase)
            || !ShouldWatchAsWorkTreeRoot(path))
        {
            return;
        }

        lock (_lock)
        {
            if (_disposed || !_watchedWorkTreePaths.Add(path))
            {
                return;
            }

            AddWatcher(path, "*", includeSubdirectories: true);
        }
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

}
