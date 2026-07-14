using ExpressThat.LovelyGit.Services.Git.CommitGraph.Models;

namespace ExpressThat.LovelyGit.Services.Git.CommitGraph;

internal interface ICommitFileDiffCache
{
    Task<CommitFileDiffResponse?> GetAsync(
        Guid repositoryId,
        string commitHash,
        string path,
        CommitDiffViewMode viewMode,
        bool ignoreWhitespace,
        CancellationToken cancellationToken);

    Task<bool> HasAsync(
        Guid repositoryId,
        string commitHash,
        string path,
        CommitDiffViewMode viewMode,
        bool ignoreWhitespace,
        CancellationToken cancellationToken);

    Task SaveAsync(
        Guid repositoryId,
        string commitHash,
        string path,
        CommitFileDiffResponse response,
        bool ignoreWhitespace,
        CancellationToken cancellationToken);

    Task RemoveAsync(
        Guid repositoryId,
        string commitHash,
        string path,
        CommitDiffViewMode viewMode,
        bool ignoreWhitespace,
        CancellationToken cancellationToken);

    Task ClearAsync(Guid repositoryId, string commitHash, CancellationToken cancellationToken);
}
