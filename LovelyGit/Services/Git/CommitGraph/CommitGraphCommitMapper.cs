using ExpressThat.LovelyGit.Services.Git.CommitGraph.Models;
using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser;

namespace ExpressThat.LovelyGit.Services.Git.CommitGraph;

internal static class CommitGraphCommitMapper
{
    private const int CommitMessagePreviewChars = 160;

    public static CommitInfo BuildInfo(GitCommit commit, List<string> parents, string? remoteUrl)
    {
        var message = commit.Subject?.Trim();
        if (string.IsNullOrWhiteSpace(message))
        {
            message = commit.Body?.Trim() ?? string.Empty;
        }

        var hash = commit.Hash.ToString();
        return new CommitInfo
        {
            Hash = hash,
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
            RemoteUrl = RemoteCommitUrlBuilder.Build(remoteUrl, hash),
            RemoteRepositoryUrl = RemoteCommitUrlBuilder.BuildRepository(remoteUrl),
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
