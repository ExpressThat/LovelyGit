using System.Text;
using System.Diagnostics;
using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser;

namespace ExpressThat.LovelyGit.Services.Git.CommitSearch;

internal static partial class NativeCommitSearchReader
{
    public const int DefaultMaximumCommits = 100_000;
    public const int DefaultResultLimit = 50;
    public static readonly TimeSpan DefaultMaximumDuration = TimeSpan.FromMilliseconds(1_500);
    public const int DeepMaximumCommits = 500_000;
    public static readonly TimeSpan DeepMaximumDuration = TimeSpan.FromSeconds(8);
    private const int MaximumResultLimit = 100;

    public static async Task<CommitSearchResponse> SearchAsync(
        string repositoryPath,
        string query,
        string author,
        long? afterUnixSeconds,
        long? beforeUnixSeconds,
        int limit,
        int maximumCommits,
        TimeSpan maximumDuration,
        CancellationToken cancellationToken)
    {
        var normalizedQuery = query.Trim();
        var normalizedAuthor = author.Trim();
        var queryUtf8 = Encoding.UTF8.GetBytes(normalizedQuery);
        var authorUtf8 = Encoding.UTF8.GetBytes(normalizedAuthor);
        var resultLimit = Math.Clamp(limit, 1, MaximumResultLimit);
        var scanLimit = Math.Max(1, maximumCommits);
        var startedAt = Stopwatch.GetTimestamp();
        using var repository = await LovelyGitRepository
            .OpenAsync(repositoryPath, cancellationToken)
            .ConfigureAwait(false);
        var directHashResult = normalizedAuthor.Length == 0
            && afterUnixSeconds == null
            && beforeUnixSeconds == null
            ? await TryResolveHashAsync(
                repository, normalizedQuery, cancellationToken).ConfigureAwait(false)
            : null;
        if (directHashResult != null)
        {
            return new CommitSearchResponse
            {
                Query = normalizedQuery,
                Author = normalizedAuthor,
                AfterUnixSeconds = afterUnixSeconds,
                BeforeUnixSeconds = beforeUnixSeconds,
                Results = [directHashResult],
                ScannedCommitCount = 1,
                MatchingCommitCount = 1,
                IsPartial = false,
            };
        }
        var startingCommits = await repository
            .GetStartingCommitsAsync(cancellationToken, includeTags: true)
            .ConfigureAwait(false);
        var primaryHistory = new Stack<GitObjectId>();
        var otherHistory = new Queue<GitObjectId>(startingCommits.Count);
        var seen = new HashSet<GitObjectId>();
        foreach (var commit in startingCommits)
        {
            if (seen.Add(commit.Hash))
            {
                if (commit.Hash == repository.HeadTarget) primaryHistory.Push(commit.Hash);
                else otherHistory.Enqueue(commit.Hash);
            }
        }

        var newestMatches = new PriorityQueue<CommitSearchResult, SearchResultPriority>(
            SearchResultPriorityComparer.Instance);
        var scannedCount = 0;
        var matchingCount = 0;
        while ((primaryHistory.Count > 0 || otherHistory.Count > 0) && scannedCount < scanLimit)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if ((scannedCount & 63) == 0
                && maximumDuration != Timeout.InfiniteTimeSpan
                && Stopwatch.GetElapsedTime(startedAt) >= maximumDuration)
            {
                break;
            }

            var followsHead = primaryHistory.Count > 0;
            var hash = followsHead ? primaryHistory.Pop() : otherHistory.Dequeue();
            var header = await repository
                .GetCommitSearchHeaderAsync(
                    hash, queryUtf8, normalizedQuery, authorUtf8, normalizedAuthor, cancellationToken)
                .ConfigureAwait(false);
            scannedCount++;
            var hashMatches = normalizedQuery.Length > 0
                && hash.Value.Contains(normalizedQuery, StringComparison.OrdinalIgnoreCase);
            var afterMatches = afterUnixSeconds == null
                || header.AuthorUnixSeconds >= afterUnixSeconds.Value;
            var beforeMatches = beforeUnixSeconds == null
                || header.AuthorUnixSeconds < beforeUnixSeconds.Value;
            if ((header.TextMatches || hashMatches)
                && header.AuthorMatches
                && afterMatches
                && beforeMatches)
            {
                matchingCount++;
                var priority = new SearchResultPriority(header.AuthorUnixSeconds, hash.Value);
                if (ShouldMaterialize(newestMatches, priority, resultLimit))
                {
                    if (newestMatches.Count >= resultLimit)
                    {
                        newestMatches.Dequeue();
                    }

                    var commit = await repository
                        .GetCommitAsync(hash, cancellationToken)
                        .ConfigureAwait(false);
                    newestMatches.Enqueue(ToResult(commit, normalizedQuery), priority);
                }
            }

            if (followsHead)
            {
                for (var index = header.ParentHashCount - 1; index >= 0; index--)
                {
                    var parent = header.GetParentHash(index);
                    if (seen.Add(parent)) primaryHistory.Push(parent);
                }
            }
            else
            {
                for (var index = 0; index < header.ParentHashCount; index++)
                {
                    var parent = header.GetParentHash(index);
                    if (seen.Add(parent)) otherHistory.Enqueue(parent);
                }
            }
        }

        return new CommitSearchResponse
        {
            Query = normalizedQuery,
            Author = normalizedAuthor,
            AfterUnixSeconds = afterUnixSeconds,
            BeforeUnixSeconds = beforeUnixSeconds,
            Results = newestMatches.UnorderedItems
                .Select(item => item.Element)
                .OrderByDescending(result => result.Date)
                .ThenBy(result => result.Hash, StringComparer.Ordinal)
                .ToList(),
            ScannedCommitCount = scannedCount,
            MatchingCommitCount = matchingCount,
            IsPartial = primaryHistory.Count > 0 || otherHistory.Count > 0,
        };
    }

    private static async Task<CommitSearchResult?> TryResolveHashAsync(
        LovelyGitRepository repository,
        string query,
        CancellationToken cancellationToken)
    {
        if (query.Length < 7 || query.Any(character => !Uri.IsHexDigit(character))) return null;
        var id = await repository.ResolveUniqueObjectPrefixAsync(query, cancellationToken)
            .ConfigureAwait(false);
        if (id == null) return null;
        try
        {
            return ToResult(
                await repository.GetCommitAsync(id.Value, cancellationToken).ConfigureAwait(false),
                query);
        }
        catch (InvalidDataException)
        {
            return null;
        }
    }

}
