using ExpressThat.LovelyGit.Services.Data.Repositorys;
using ExpressThat.LovelyGit.Services.Git.CommitGraph.Models;
using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser;
using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser.Remotes;

namespace ExpressThat.LovelyGit.Services.Git.CommitGraph;

public sealed partial class CommitGraphManager : IDisposable
{
    private readonly LovelyGitRepository _repository;
    private readonly Guid _repositoryId;
    private readonly string? _remoteRepositoryUrl;
    private readonly List<string> _remotePrefixes;
    private CommitGraphTraversalSession? _session;
    private bool _disposed;

    private CommitGraphManager(
        LovelyGitRepository repository,
        Guid repositoryId,
        string? remoteUrl)
    {
        _repository = repository;
        _repositoryId = repositoryId;
        _remoteRepositoryUrl = RemoteCommitUrlBuilder.BuildRepository(remoteUrl);
        _remotePrefixes = repository.RemotePrefixes.ToList();
    }

    public int CommitCount => -1;

    public static async Task<CommitGraphOpenResult> TryOpenAsync(
        string gitDirOrWorkTreePath,
        Guid repositoryId,
        CommitGraphRepository commitGraphRepository,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(gitDirOrWorkTreePath))
        {
            return new CommitGraphOpenResult(false, null, "Path is null or empty.");
        }

        try
        {
            var repository = await LovelyGitRepository.OpenAsync(gitDirOrWorkTreePath, cancellationToken)
                .ConfigureAwait(false);
            var remoteUrl = await GitRemoteConfigReader
                .ReadPrimaryRemoteUrlAsync(repository.GitDirectory, cancellationToken)
                .ConfigureAwait(false);
            return new CommitGraphOpenResult(
                true,
                new CommitGraphManager(repository, repositoryId, remoteUrl),
                null);
        }
        catch (Exception ex)
        {
            return new CommitGraphOpenResult(false, null, ex.Message);
        }
    }

    public async Task<CommitGraphPageResult> GetCommitGraphPageAsync(
        CommitGraphCursorState cursor,
        int limit,
        CancellationToken cancellationToken = default)
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(CommitGraphManager));
        }

        var session = await OpenTraversalSessionAsync(cursor, cancellationToken).ConfigureAwait(false);
        var response = await ReadRowsAsync(session, Math.Max(limit, 0), true, cancellationToken)
            .ConfigureAwait(false);
        _repository.ClearObjectCaches();

        if (!response.HasMore)
        {
            _session = null;
        }

        var nextCursor = new CommitGraphCursorState(response.HasMore ? session.RepositoryId : null, session.Offset);
        return new CommitGraphPageResult(response, nextCursor);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _session = null;
        _repository.Dispose();
        _disposed = true;
    }
}
