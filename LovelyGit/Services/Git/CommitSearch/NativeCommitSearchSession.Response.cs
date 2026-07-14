using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser;

namespace ExpressThat.LovelyGit.Services.Git.CommitSearch;

internal sealed partial class NativeCommitSearchSession
{
    private CommitSearchResponse BuildResponse() => new()
    {
        Query = _query,
        Author = _author,
        Scope = _scope,
        AfterUnixSeconds = _afterUnixSeconds,
        BeforeUnixSeconds = _beforeUnixSeconds,
        Results = _newestMatches.UnorderedItems.Select(item => item.Element)
            .OrderByDescending(result => result.Date)
            .ThenBy(result => result.Hash, StringComparer.Ordinal)
            .ToList(),
        ScannedCommitCount = _scannedCount,
        MatchingCommitCount = _matchingCount,
        IsPartial = HasPending,
    };

    private CommitSearchResponse BuildDirectResponse() => new()
    {
        Query = _query,
        Author = _author,
        Scope = _scope,
        Results = [_directResult!],
        ScannedCommitCount = 1,
        MatchingCommitCount = 1,
        IsPartial = false,
    };

    private bool HasPending => _primaryHistory.Count > 0 || _otherHistory.Count > 0;

    public void Dispose()
    {
        if (_disposed) return;
        _repository.Dispose();
        _disposed = true;
    }

    private static StringComparison PathComparison =>
        OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

    private static async Task<IReadOnlyList<GitCommit>> ReadStartingCommitsAsync(
        LovelyGitRepository repository,
        string scope,
        CancellationToken cancellationToken)
    {
        if (scope.Length == 0)
        {
            return await repository.GetStartingCommitsAsync(
                cancellationToken, includeTags: true).ConfigureAwait(false);
        }
        if (!repository.TryResolveRefTarget(scope, out var target))
        {
            throw new ArgumentException($"Git ref not found: {scope}", nameof(scope));
        }
        return [await repository.GetGraphCommitAsync(target, cancellationToken).ConfigureAwait(false)];
    }
}
