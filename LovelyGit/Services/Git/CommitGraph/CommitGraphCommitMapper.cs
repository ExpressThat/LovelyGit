using ExpressThat.LovelyGit.Services.Git.CommitGraph.Models;
using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser;

namespace ExpressThat.LovelyGit.Services.Git.CommitGraph;

internal static class CommitGraphCommitMapper
{
    private const int CommitMessagePreviewChars = 160;

    public static CommitInfo BuildInfo(GitCommit commit, string? remoteRepositoryUrl)
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
            Author = string.IsNullOrWhiteSpace(commit.AuthorName) ? "unknown" : commit.AuthorName,
            Email = commit.AuthorEmail,
            Date = commit.AuthorUnixSeconds,
            Message = TruncateMessage(message),
            Refs = commit.Refs.Count == 0
                ? CommitGraphEmptyLists.Refs
                : commit.Refs.Select(reference => new CommitRefInfo
                {
                    Name = reference.Name,
                    Kind = reference.Kind switch
                    {
                        GitRefKind.Remote => CommitRefKind.Remote,
                        GitRefKind.Tag => CommitRefKind.Tag,
                        GitRefKind.Stash => CommitRefKind.Stash,
                        _ => CommitRefKind.Local,
                    },
                    RemoteUrl = reference.Kind == GitRefKind.Tag
                        ? RemoteCommitUrlBuilder.BuildTagFromRepository(
                            remoteRepositoryUrl,
                            reference.Name)
                        : null,
                })
                .ToList(),
            Stats = null,
            SignatureKind = MapSignatureKind(commit.SignatureKind),
        };
    }

    public static long GetGraphUnixTimeSeconds(GitCommit commit)
    {
        return commit.CommitterUnixSeconds == 0
            ? commit.AuthorUnixSeconds
            : commit.CommitterUnixSeconds;
    }

    private static string TruncateMessage(string value)
    {
        return value.Length <= CommitMessagePreviewChars ? value : value[..CommitMessagePreviewChars];
    }

    internal static CommitSignatureKind MapSignatureKind(GitSignatureKind kind) => kind switch
    {
        GitSignatureKind.OpenPgp => CommitSignatureKind.OpenPgp,
        GitSignatureKind.Ssh => CommitSignatureKind.Ssh,
        GitSignatureKind.X509 => CommitSignatureKind.X509,
        GitSignatureKind.Unknown => CommitSignatureKind.Unknown,
        _ => CommitSignatureKind.None,
    };
}
