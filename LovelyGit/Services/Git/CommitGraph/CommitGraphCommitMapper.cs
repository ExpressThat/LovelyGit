using ExpressThat.LovelyGit.Services.Git.CommitGraph.Models;
using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser;

namespace ExpressThat.LovelyGit.Services.Git.CommitGraph;

internal static class CommitGraphCommitMapper
{
    private const int CommitMessagePreviewChars = 160;

    public static CommitInfo BuildInfo(GitCommit commit, List<string> parents)
    {
        var message = commit.Subject?.Trim();
        if (string.IsNullOrWhiteSpace(message))
        {
            message = commit.Body?.Trim() ?? string.Empty;
        }

        return new CommitInfo
        {
            Hash = commit.Hash.ToString(),
            Parents = parents,
            Author = string.IsNullOrWhiteSpace(commit.AuthorName) ? "unknown" : commit.AuthorName,
            Email = commit.AuthorEmail,
            Date = commit.AuthorUnixSeconds,
            Message = TruncateMessage(message),
            Branches = commit.Branches.ToList(),
            Tags = commit.Tags.ToList(),
            Refs = commit.Refs
                .Select(reference => new CommitRefInfo
                {
                    Name = reference.Name,
                    Kind = reference.Kind switch
                    {
                        GitRefKind.Remote => CommitRefKind.Remote,
                        GitRefKind.Tag => CommitRefKind.Tag,
                        _ => CommitRefKind.Local,
                    },
                })
                .ToList(),
            Stats = null,
        };
    }

    public static long GetAuthorUnixTimeSeconds(GitCommit commit)
    {
        return commit.AuthorUnixSeconds;
    }

    private static string TruncateMessage(string value)
    {
        return value.Length <= CommitMessagePreviewChars ? value : value[..CommitMessagePreviewChars];
    }
}
