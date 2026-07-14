using ExpressThat.LovelyGit.Services.Data.Repositorys;
using ExpressThat.LovelyGit.Services.Git.CommitGraph.Models;

namespace ExpressThat.LovelyGit.Services.Git.CommitGraph;

internal sealed class CommitFileDiffCacheAdapter(CommitGraphRepository repository)
    : ICommitFileDiffCache
{
    public Task<CommitFileDiffResponse?> GetAsync(
        Guid repositoryId,
        string commitHash,
        string path,
        CommitDiffViewMode viewMode,
        bool ignoreWhitespace,
        CancellationToken cancellationToken) =>
        repository.GetCommitFileDiffAsync(
            repositoryId,
            commitHash,
            path,
            viewMode,
            ignoreWhitespace,
            cancellationToken);

    public Task<bool> HasAsync(
        Guid repositoryId,
        string commitHash,
        string path,
        CommitDiffViewMode viewMode,
        bool ignoreWhitespace,
        CancellationToken cancellationToken) =>
        repository.HasCommitFileDiffAsync(
            repositoryId,
            commitHash,
            path,
            viewMode,
            ignoreWhitespace,
            cancellationToken);

    public Task SaveAsync(
        Guid repositoryId,
        string commitHash,
        string path,
        CommitFileDiffResponse response,
        bool ignoreWhitespace,
        CancellationToken cancellationToken) =>
        repository.SaveCommitFileDiffAsync(
            repositoryId,
            commitHash,
            path,
            response,
            ignoreWhitespace,
            cancellationToken);

    public Task RemoveAsync(
        Guid repositoryId,
        string commitHash,
        string path,
        CommitDiffViewMode viewMode,
        bool ignoreWhitespace,
        CancellationToken cancellationToken) =>
        repository.RemoveCommitFileDiffAsync(
            repositoryId,
            commitHash,
            path,
            viewMode,
            ignoreWhitespace,
            cancellationToken);

    public Task ClearAsync(
        Guid repositoryId,
        string commitHash,
        CancellationToken cancellationToken) =>
        repository.ClearCommitFileDiffsAsync(repositoryId, commitHash, cancellationToken);
}
