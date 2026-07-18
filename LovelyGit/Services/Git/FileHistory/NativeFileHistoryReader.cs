using System.Diagnostics;
using System.Text;
using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser;

namespace ExpressThat.LovelyGit.Services.Git.FileHistory;

internal static partial class NativeFileHistoryReader
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
        cancellationToken.ThrowIfCancellationRequested();
        var normalizedPath = FileHistoryPath.Normalize(path);
        using var repository = await LovelyGitRepository
            .OpenObjectDatabaseAsync(repositoryPath, cancellationToken)
            .ConfigureAwait(false);
        var start = await ResolveStartAsync(repository, startCommitHash, cancellationToken)
            .ConfigureAwait(false);
        if (start == null)
        {
            return new FileHistoryResponse { Path = normalizedPath };
        }

        var encodedPaths = new Dictionary<string, byte[]>(StringComparer.Ordinal);
        var encodedPath = GetEncodedPath(encodedPaths, normalizedPath);
        var header = await repository.GetCommitAncestryHeaderAsync(start.Value, cancellationToken)
            .ConfigureAwait(false);
        var current = header.TreeHash == null
            ? null
            : await repository.TryGetTreeFileAsync(
                    header.TreeHash.Value, normalizedPath, encodedPath, cancellationToken)
                .ConfigureAwait(false);
        var pending = new Queue<HistoryWorkItem>();
        var seen = new HashSet<HistoryKey>();
        Enqueue(
            pending,
            seen,
            new HistoryWorkItem(start.Value, normalizedPath, encodedPath, header, current));
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
            scanned++;
            var change = await InspectParentsAsync(
                    repository, item, encodedPaths, pending, seen, cancellationToken)
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

    private static async Task<GitObjectId?> ResolveStartAsync(
        LovelyGitRepository repository,
        string? hash,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(hash))
        {
            return await repository.ResolveHeadAsync(cancellationToken).ConfigureAwait(false);
        }

        return GitObjectId.TryParse(hash.Trim(), repository.ObjectFormat, out var id)
            ? id
            : throw new ArgumentException("The starting commit hash is invalid.", nameof(hash));
    }

    private static async Task<FileChange?> InspectParentsAsync(
        LovelyGitRepository repository,
        HistoryWorkItem item,
        Dictionary<string, byte[]> encodedPaths,
        Queue<HistoryWorkItem> pending,
        HashSet<HistoryKey> seen,
        CancellationToken cancellationToken)
    {
        if (item.Header.ParentHashCount == 0)
        {
            return item.Current == null ? null : new FileChange(FileHistoryChangeKind.Added, null);
        }

        FileChange? firstChange = null;
        for (var index = 0; index < item.Header.ParentHashCount; index++)
        {
            var parentHash = item.Header.GetParentHash(index);
            var parentHeader = await repository.GetCommitAncestryHeaderAsync(parentHash, cancellationToken)
                .ConfigureAwait(false);
            var parent = parentHeader.TreeHash == null
                ? null
                : await repository.TryGetTreeFileAsync(
                        parentHeader.TreeHash.Value, item.Path, item.EncodedPath, cancellationToken)
                    .ConfigureAwait(false);
            var edge = await ClassifyAsync(
                    repository,
                    item.Current,
                    item.Header.TreeHash,
                    parent,
                    parentHeader.TreeHash,
                    cancellationToken)
                .ConfigureAwait(false);
            var nextPath = edge.PreviousPath ?? item.Path;
            var nextFile = edge.PreviousFile ?? parent;
            if (edge.PreviousPath != null || parent != null || item.Current == null)
            {
                Enqueue(
                    pending,
                    seen,
                    new HistoryWorkItem(
                        parentHash,
                        nextPath,
                        GetEncodedPath(encodedPaths, nextPath),
                        parentHeader,
                        nextFile));
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
            return new EdgeResult(
                parent == null ? null : new FileChange(FileHistoryChangeKind.Deleted, null),
                null,
                null);
        }

        if (parent != null)
        {
            FileChange? changed = current.ObjectId == parent.ObjectId && current.Mode == parent.Mode
                ? null
                : new FileChange(current.Mode == parent.Mode
                    ? FileHistoryChangeKind.Modified
                    : FileHistoryChangeKind.TypeChanged, null);
            return new EdgeResult(changed, null, null);
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
                    new FileChange(FileHistoryChangeKind.Renamed, candidate.Path),
                    candidate.Path,
                    candidate);
            }
        }

        return new EdgeResult(new FileChange(FileHistoryChangeKind.Added, null), null, null);
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
        Queue<HistoryWorkItem> pending, HashSet<HistoryKey> seen, HistoryWorkItem item)
    {
        if (seen.Add(new HistoryKey(item.Hash, item.Path)))
        {
            pending.Enqueue(item);
        }
    }

    private static ReadOnlyMemory<byte> GetEncodedPath(
        Dictionary<string, byte[]> encodedPaths,
        string path)
    {
        if (!encodedPaths.TryGetValue(path, out var encoded))
        {
            encoded = Encoding.UTF8.GetBytes(path);
            encodedPaths.Add(path, encoded);
        }
        return encoded;
    }

}
