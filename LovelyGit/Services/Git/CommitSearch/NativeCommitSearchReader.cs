using System.Text;
using System.Diagnostics;
using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser;

namespace ExpressThat.LovelyGit.Services.Git.CommitSearch;

internal static class NativeCommitSearchReader
{
    public const int DefaultMaximumCommits = 100_000;
    public const int DefaultResultLimit = 50;
    public static readonly TimeSpan DefaultMaximumDuration = TimeSpan.FromMilliseconds(1_500);
    public const int DeepMaximumCommits = 500_000;
    public static readonly TimeSpan DeepMaximumDuration = TimeSpan.FromSeconds(8);
    private const int MaximumResultLimit = 100;
    private const int PreviewLength = 180;

    public static async Task<CommitSearchResponse> SearchAsync(
        string repositoryPath,
        string query,
        int limit,
        int maximumCommits,
        TimeSpan maximumDuration,
        CancellationToken cancellationToken)
    {
        var normalizedQuery = query.Trim();
        var queryUtf8 = Encoding.UTF8.GetBytes(normalizedQuery);
        var resultLimit = Math.Clamp(limit, 1, MaximumResultLimit);
        var scanLimit = Math.Max(1, maximumCommits);
        var startedAt = Stopwatch.GetTimestamp();
        using var repository = await LovelyGitRepository
            .OpenAsync(repositoryPath, cancellationToken)
            .ConfigureAwait(false);
        var startingCommits = await repository
            .GetStartingCommitsAsync(cancellationToken, includeTags: true)
            .ConfigureAwait(false);
        var pending = new Queue<GitObjectId>(startingCommits.Count);
        var seen = new HashSet<GitObjectId>();
        foreach (var commit in startingCommits)
        {
            if (seen.Add(commit.Hash))
            {
                pending.Enqueue(commit.Hash);
            }
        }

        var newestMatches = new PriorityQueue<CommitSearchResult, SearchResultPriority>(
            SearchResultPriorityComparer.Instance);
        var scannedCount = 0;
        var matchingCount = 0;
        while (pending.Count > 0 && scannedCount < scanLimit)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if ((scannedCount & 63) == 0
                && maximumDuration != Timeout.InfiniteTimeSpan
                && Stopwatch.GetElapsedTime(startedAt) >= maximumDuration)
            {
                break;
            }

            var hash = pending.Dequeue();
            var header = await repository
                .GetCommitSearchHeaderAsync(hash, queryUtf8, normalizedQuery, cancellationToken)
                .ConfigureAwait(false);
            scannedCount++;
            if (header.IsMatch || hash.Value.Contains(normalizedQuery, StringComparison.OrdinalIgnoreCase))
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

            for (var index = 0; index < header.ParentHashCount; index++)
            {
                var parent = header.GetParentHash(index);
                if (seen.Add(parent))
                {
                    pending.Enqueue(parent);
                }
            }
        }

        return new CommitSearchResponse
        {
            Query = normalizedQuery,
            Results = newestMatches.UnorderedItems
                .Select(item => item.Element)
                .OrderByDescending(result => result.Date)
                .ThenBy(result => result.Hash, StringComparer.Ordinal)
                .ToList(),
            ScannedCommitCount = scannedCount,
            MatchingCommitCount = matchingCount,
            IsPartial = pending.Count > 0,
        };
    }

    private static bool ShouldMaterialize(
        PriorityQueue<CommitSearchResult, SearchResultPriority> results,
        SearchResultPriority priority,
        int limit)
    {
        return results.Count < limit
            || !results.TryPeek(out _, out var oldest)
            || SearchResultPriorityComparer.Instance.Compare(priority, oldest) > 0;
    }

    private static CommitSearchResult ToResult(GitCommit commit, string query)
    {
        var subject = string.IsNullOrWhiteSpace(commit.Subject)
            ? "(no commit message)"
            : commit.Subject.Trim();
        var bodyMatchIndex = commit.Body.IndexOf(query, StringComparison.OrdinalIgnoreCase);
        return new CommitSearchResult
        {
            Hash = commit.Hash.Value,
            Author = string.IsNullOrWhiteSpace(commit.AuthorName) ? "unknown" : commit.AuthorName,
            Email = commit.AuthorEmail,
            Date = commit.AuthorUnixSeconds,
            Subject = subject,
            Preview = bodyMatchIndex >= 0
                ? BuildPreview(commit.Body, bodyMatchIndex)
                : subject,
            Refs = commit.Refs.Select(reference => reference.Name).Take(4).ToList(),
        };
    }

    private static string BuildPreview(string body, int matchIndex)
    {
        var start = Math.Max(0, matchIndex - (PreviewLength / 3));
        var length = Math.Min(PreviewLength, body.Length - start);
        var source = body.AsSpan(start, length).Trim();
        var builder = new StringBuilder(source.Length + 2);
        var whitespace = false;
        foreach (var character in source)
        {
            if (char.IsWhiteSpace(character))
            {
                whitespace = builder.Length > 0;
            }
            else
            {
                if (whitespace)
                {
                    builder.Append(' ');
                    whitespace = false;
                }

                builder.Append(character);
            }
        }

        if (start > 0)
        {
            builder.Insert(0, '…');
        }

        if (start + length < body.Length)
        {
            builder.Append('…');
        }

        return builder.ToString();
    }

    private readonly record struct SearchResultPriority(long Date, string Hash);

    private sealed class SearchResultPriorityComparer : IComparer<SearchResultPriority>
    {
        public static SearchResultPriorityComparer Instance { get; } = new();

        public int Compare(SearchResultPriority left, SearchResultPriority right)
        {
            var date = left.Date.CompareTo(right.Date);
            return date != 0 ? date : string.CompareOrdinal(right.Hash, left.Hash);
        }
    }
}
