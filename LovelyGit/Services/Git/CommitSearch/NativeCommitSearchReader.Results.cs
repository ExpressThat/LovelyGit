using System.Text;
using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser;

namespace ExpressThat.LovelyGit.Services.Git.CommitSearch;

internal static partial class NativeCommitSearchReader
{
    private const int PreviewLength = 180;

    internal static bool ShouldMaterialize(
        PriorityQueue<CommitSearchResult, SearchResultPriority> results,
        SearchResultPriority priority,
        int limit) =>
        results.Count < limit
        || !results.TryPeek(out _, out var oldest)
        || SearchResultPriorityComparer.Instance.Compare(priority, oldest) > 0;

    internal static CommitSearchResult ToResult(GitCommit commit, string query)
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
            if (char.IsWhiteSpace(character)) whitespace = builder.Length > 0;
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

        if (start > 0) builder.Insert(0, '…');
        if (start + length < body.Length) builder.Append('…');
        return builder.ToString();
    }

    internal readonly record struct SearchResultPriority(long Date, string Hash);

    internal sealed class SearchResultPriorityComparer : IComparer<SearchResultPriority>
    {
        public static SearchResultPriorityComparer Instance { get; } = new();

        public int Compare(SearchResultPriority left, SearchResultPriority right)
        {
            var date = left.Date.CompareTo(right.Date);
            return date != 0 ? date : string.CompareOrdinal(right.Hash, left.Hash);
        }
    }
}
