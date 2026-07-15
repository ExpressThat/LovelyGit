using System.Diagnostics;
using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser;

namespace ExpressThat.LovelyGit.Services.Git.FileHistory;

internal static class NativeFileHistoryReader
{
    public const int DefaultMaximumCommits = 100_000;
    public const int DeepMaximumCommits = 500_000;
    public const int DefaultResultLimit = 100;
    public static readonly TimeSpan DefaultMaximumDuration = TimeSpan.FromMilliseconds(1_500);
    public static readonly TimeSpan DeepMaximumDuration = TimeSpan.FromSeconds(8);
    private const int MaximumResultLimit = 250;

    public static async Task<FileHistoryResponse> ReadAsync(
        string repositoryPath,
        string path,
        string? startCommitHash,
        int limit,
        int maximumCommits,
        TimeSpan maximumDuration,
        CancellationToken cancellationToken)
    {
        var normalizedPath = FileHistoryPath.Normalize(path);
        using var repository = await LovelyGitRepository.OpenAsync(repositoryPath, cancellationToken)
            .ConfigureAwait(false);
        var start = ResolveStart(repository, startCommitHash);
        if (start == null)
        {
            return new FileHistoryResponse { Path = normalizedPath };
        }

        var pending = new Queue<HistoryWorkItem>();
        var seen = new HashSet<HistoryWorkItem>();
        Enqueue(pending, seen, new HistoryWorkItem(start.Value, normalizedPath));
        var results = new List<FileHistoryResult>(Math.Clamp(limit, 1, MaximumResultLimit));
        var emitted = new HashSet<GitObjectId>();
        var startedAt = Stopwatch.GetTimestamp();
        var scanned = 0;
        var matched = 0;
        while (pending.Count > 0 && scanned < Math.Max(1, maximumCommits))
        {
            cancellationToken.ThrowIfCancellationRequested();
            if ((scanned & 63) == 0 && maximumDuration != Timeout.InfiniteTimeSpan
                && Stopwatch.GetElapsedTime(startedAt) >= maximumDuration)
            {
                break;
            }

            var item = pending.Dequeue();
            var header = await repository.GetCommitAncestryHeaderAsync(item.Hash, cancellationToken)
                .ConfigureAwait(false);
            scanned++;
            var current = header.TreeHash == null
                ? null
                : await repository.TryGetTreeFileAsync(header.TreeHash.Value, item.Path, cancellationToken)
                    .ConfigureAwait(false);
            var change = await InspectParentsAsync(repository, item, header, current, pending, seen, cancellationToken)
                .ConfigureAwait(false);
            if (change == null || !emitted.Add(item.Hash))
            {
                continue;
            }

            matched++;
            if (results.Count < Math.Clamp(limit, 1, MaximumResultLimit))
            {
                var commit = await repository.GetCommitAsync(item.Hash, cancellationToken).ConfigureAwait(false);
                results.Add(ToResult(commit, item.Path, change.Value));
            }
        }

        return new FileHistoryResponse
        {
            Path = normalizedPath,
            Results = results.OrderByDescending(result => result.Date).ToList(),
            ScannedCommitCount = scanned,
            MatchingCommitCount = matched,
            IsPartial = pending.Count > 0,
        };
    }

    private static GitObjectId? ResolveStart(LovelyGitRepository repository, string? hash)
    {
        if (string.IsNullOrWhiteSpace(hash))
        {
            return repository.HeadTarget;
        }

        return GitObjectId.TryParse(hash.Trim(), repository.ObjectFormat, out var id)
            ? id
            : throw new ArgumentException("The starting commit hash is invalid.", nameof(hash));
    }

    private static async Task<FileChange?> InspectParentsAsync(
        LovelyGitRepository repository,
        HistoryWorkItem item,
        GitCommitAncestryHeader header,
        GitTreeFile? current,
        Queue<HistoryWorkItem> pending,
        HashSet<HistoryWorkItem> seen,
        CancellationToken cancellationToken)
    {
        if (header.ParentHashCount == 0)
        {
            return current == null ? null : new FileChange(FileHistoryChangeKind.Added, null);
        }

        FileChange? firstChange = null;
        for (var index = 0; index < header.ParentHashCount; index++)
        {
            var parentHash = header.GetParentHash(index);
            var parentHeader = await repository.GetCommitAncestryHeaderAsync(parentHash, cancellationToken)
                .ConfigureAwait(false);
            var parent = parentHeader.TreeHash == null
                ? null
                : await repository.TryGetTreeFileAsync(parentHeader.TreeHash.Value, item.Path, cancellationToken)
                    .ConfigureAwait(false);
            var edge = await ClassifyAsync(
                    repository, current, header.TreeHash, parent, parentHeader.TreeHash, cancellationToken)
                .ConfigureAwait(false);
            if (edge.PreviousPath != null)
            {
                Enqueue(pending, seen, new HistoryWorkItem(parentHash, edge.PreviousPath));
            }
            else if (parent != null || current == null)
            {
                Enqueue(pending, seen, new HistoryWorkItem(parentHash, item.Path));
            }

            firstChange ??= edge.Change;
        }

        return firstChange;
    }

    private static async Task<EdgeResult> ClassifyAsync(
        LovelyGitRepository repository,
        GitTreeFile? current,
        GitObjectId? currentTree,
        GitTreeFile? parent,
        GitObjectId? parentTree,
        CancellationToken cancellationToken)
    {
        if (current == null)
        {
            return new EdgeResult(parent == null ? null : new FileChange(FileHistoryChangeKind.Deleted, null), null);
        }

        if (parent != null)
        {
            FileChange? changed = current.ObjectId == parent.ObjectId && current.Mode == parent.Mode
                ? null
                : new FileChange(current.Mode == parent.Mode
                    ? FileHistoryChangeKind.Modified
                    : FileHistoryChangeKind.TypeChanged, null);
            return new EdgeResult(changed, null);
        }

        if (parentTree != null)
        {
            var candidate = await repository.FindTreeFileByObjectIdAsync(
                    parentTree.Value, current.ObjectId, cancellationToken)
                .ConfigureAwait(false);
            if (candidate != null && currentTree != null
                && await repository.TryGetTreeFileAsync(currentTree.Value, candidate.Path, cancellationToken)
                    .ConfigureAwait(false) == null)
            {
                return new EdgeResult(
                    new FileChange(FileHistoryChangeKind.Renamed, candidate.Path), candidate.Path);
            }
        }

        return new EdgeResult(new FileChange(FileHistoryChangeKind.Added, null), null);
    }

    private static FileHistoryResult ToResult(GitCommit commit, string path, FileChange change) => new()
    {
        Hash = commit.Hash.Value,
        Author = string.IsNullOrWhiteSpace(commit.AuthorName) ? "unknown" : commit.AuthorName,
        Email = commit.AuthorEmail,
        Date = commit.AuthorUnixSeconds,
        Subject = string.IsNullOrWhiteSpace(commit.Subject) ? "(no commit message)" : commit.Subject.Trim(),
        Path = path,
        PreviousPath = change.PreviousPath,
        ChangeKind = change.Kind,
    };

    private static void Enqueue(
        Queue<HistoryWorkItem> pending, HashSet<HistoryWorkItem> seen, HistoryWorkItem item)
    {
        if (seen.Add(item))
        {
            pending.Enqueue(item);
        }
    }

    private readonly record struct HistoryWorkItem(GitObjectId Hash, string Path);
    private readonly record struct FileChange(FileHistoryChangeKind Kind, string? PreviousPath);
    private readonly record struct EdgeResult(FileChange? Change, string? PreviousPath);
}
