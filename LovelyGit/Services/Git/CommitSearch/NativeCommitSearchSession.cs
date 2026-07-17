using System.Diagnostics;
using System.Text;
using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser;
using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser.Refs;

namespace ExpressThat.LovelyGit.Services.Git.CommitSearch;

internal sealed partial class NativeCommitSearchSession : IDisposable
{
    private readonly LovelyGitRepository _repository;
    private readonly string _repositoryPath;
    private readonly string _query;
    private readonly string _author;
    private readonly string _scope;
    private readonly long? _afterUnixSeconds;
    private readonly long? _beforeUnixSeconds;
    private readonly int _resultLimit;
    private readonly GitRefReader.RefFingerprint _refFingerprint;
    private readonly byte[] _queryUtf8;
    private readonly byte[] _authorUtf8;
    private readonly Stack<GitObjectId> _primaryHistory = [];
    private readonly Queue<GitObjectId> _otherHistory;
    private readonly HashSet<GitObjectId> _seen = [];
    private readonly PriorityQueue<CommitSearchResult, NativeCommitSearchReader.SearchResultPriority>
        _newestMatches = new(NativeCommitSearchReader.SearchResultPriorityComparer.Instance);
    private readonly CommitSearchResult? _directResult;
    private readonly long _openedAt;
    private int _scannedCount;
    private int _matchingCount;
    private bool _hasScanStarted;
    private bool _disposed;

    private NativeCommitSearchSession(
        LovelyGitRepository repository,
        string repositoryPath,
        string query,
        string author,
        string scope,
        long? afterUnixSeconds,
        long? beforeUnixSeconds,
        int resultLimit,
        CommitSearchResult? directResult,
        IReadOnlyList<GitCommit> startingCommits,
        long openedAt)
    {
        _repository = repository;
        _repositoryPath = Path.GetFullPath(repositoryPath);
        _query = query;
        _author = author;
        _scope = scope;
        _afterUnixSeconds = afterUnixSeconds;
        _beforeUnixSeconds = beforeUnixSeconds;
        _resultLimit = resultLimit;
        _refFingerprint = GitRefReader.CreateFingerprint(
            repository.GitDirectory,
            repository.WorktreeGitDirectory,
            objectFormat: repository.ObjectFormat);
        _queryUtf8 = Encoding.UTF8.GetBytes(query);
        _authorUtf8 = Encoding.UTF8.GetBytes(author);
        _directResult = directResult;
        _openedAt = openedAt;
        _otherHistory = new Queue<GitObjectId>(startingCommits.Count);
        foreach (var commit in startingCommits)
        {
            if (!_seen.Add(commit.Hash)) continue;
            if (commit.Hash == repository.HeadTarget) _primaryHistory.Push(commit.Hash);
            else _otherHistory.Enqueue(commit.Hash);
        }
    }

    public static async Task<NativeCommitSearchSession> OpenAsync(
        string repositoryPath,
        string query,
        string author,
        string scope,
        long? afterUnixSeconds,
        long? beforeUnixSeconds,
        int limit,
        CancellationToken cancellationToken)
    {
        var openedAt = Stopwatch.GetTimestamp();
        var normalizedQuery = query.Trim();
        var normalizedAuthor = author.Trim();
        var normalizedScope = scope.Trim();
        var repository = await LovelyGitRepository
            .OpenAsync(repositoryPath, cancellationToken)
            .ConfigureAwait(false);
        try
        {
            var directResult = normalizedAuthor.Length == 0
                && normalizedScope.Length == 0
                && afterUnixSeconds == null
                && beforeUnixSeconds == null
                ? await NativeCommitSearchReader.TryResolveHashAsync(
                    repository, normalizedQuery, cancellationToken).ConfigureAwait(false)
                : null;
            var startingCommits = directResult == null
                ? await ReadStartingCommitsAsync(
                    repository, normalizedScope, cancellationToken).ConfigureAwait(false)
                : [];
            return new NativeCommitSearchSession(
                repository,
                repositoryPath,
                normalizedQuery,
                normalizedAuthor,
                normalizedScope,
                afterUnixSeconds,
                beforeUnixSeconds,
                Math.Clamp(limit, 1, NativeCommitSearchReader.MaximumResultLimit),
                directResult,
                startingCommits,
                openedAt);
        }
        catch
        {
            repository.Dispose();
            throw;
        }
    }

    public bool Matches(
        string repositoryPath,
        string query,
        string author,
        string scope,
        long? afterUnixSeconds,
        long? beforeUnixSeconds,
        int limit)
    {
        try
        {
            return string.Equals(_repositoryPath, Path.GetFullPath(repositoryPath), PathComparison)
                && _query == query.Trim()
                && _author == author.Trim()
                && _scope == scope.Trim()
                && _afterUnixSeconds == afterUnixSeconds
                && _beforeUnixSeconds == beforeUnixSeconds
                && _resultLimit == Math.Clamp(
                    limit, 1, NativeCommitSearchReader.MaximumResultLimit)
                && _refFingerprint == GitRefReader.CreateFingerprint(
                    _repository.GitDirectory,
                    _repository.WorktreeGitDirectory,
                    objectFormat: _repository.ObjectFormat);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return false;
        }
    }

    public async Task<CommitSearchResponse> ScanAsync(
        int maximumCommits,
        TimeSpan maximumDuration,
        CancellationToken cancellationToken,
        int? responsiveMatchScanCount = null)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_directResult != null) return BuildDirectResponse();
        var scanLimit = Math.Max(1, maximumCommits);
        var startedAt = _hasScanStarted ? Stopwatch.GetTimestamp() : _openedAt;
        _hasScanStarted = true;
        var scannedAtStart = _scannedCount;
        var matchesAtStart = _matchingCount;
        while (HasPending && _scannedCount < scanLimit)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (((_scannedCount - scannedAtStart) & 63) == 0
                && maximumDuration != Timeout.InfiniteTimeSpan
                && Stopwatch.GetElapsedTime(startedAt) >= maximumDuration)
            {
                break;
            }

            await ScanNextAsync(cancellationToken).ConfigureAwait(false);
            if (_matchingCount > matchesAtStart
                && responsiveMatchScanCount is > 0
                && _scannedCount - scannedAtStart >= responsiveMatchScanCount.Value)
            {
                break;
            }
        }

        return BuildResponse();
    }

    private async Task ScanNextAsync(CancellationToken cancellationToken)
    {
        var followsHead = _primaryHistory.Count > 0;
        var hash = followsHead ? _primaryHistory.Pop() : _otherHistory.Dequeue();
        var header = await _repository.GetCommitSearchHeaderAsync(
            hash, _queryUtf8, _query, _authorUtf8, _author, cancellationToken).ConfigureAwait(false);
        _scannedCount++;
        var matches = (header.TextMatches
                || (_query.Length > 0
                    && hash.Value.Contains(_query, StringComparison.OrdinalIgnoreCase)))
            && header.AuthorMatches
            && (_afterUnixSeconds == null || header.AuthorUnixSeconds >= _afterUnixSeconds.Value)
            && (_beforeUnixSeconds == null || header.AuthorUnixSeconds < _beforeUnixSeconds.Value);
        if (matches) await AddMatchAsync(hash, header.AuthorUnixSeconds, cancellationToken)
            .ConfigureAwait(false);
        AddParents(header, followsHead);
    }

    private async Task AddMatchAsync(
        GitObjectId hash,
        long authorUnixSeconds,
        CancellationToken cancellationToken)
    {
        _matchingCount++;
        var priority = new NativeCommitSearchReader.SearchResultPriority(
            authorUnixSeconds, hash.Value);
        if (!NativeCommitSearchReader.ShouldMaterialize(
            _newestMatches, priority, _resultLimit)) return;
        if (_newestMatches.Count >= _resultLimit) _newestMatches.Dequeue();
        var commit = await _repository.GetCommitAsync(hash, cancellationToken).ConfigureAwait(false);
        _newestMatches.Enqueue(NativeCommitSearchReader.ToResult(commit, _query), priority);
    }

    private void AddParents(GitCommitSearchHeader header, bool followsHead)
    {
        if (followsHead)
        {
            for (var index = header.ParentHashCount - 1; index >= 0; index--)
            {
                var parent = header.GetParentHash(index);
                if (_seen.Add(parent)) _primaryHistory.Push(parent);
            }
            return;
        }

        for (var index = 0; index < header.ParentHashCount; index++)
        {
            var parent = header.GetParentHash(index);
            if (_seen.Add(parent)) _otherHistory.Enqueue(parent);
        }
    }

}
