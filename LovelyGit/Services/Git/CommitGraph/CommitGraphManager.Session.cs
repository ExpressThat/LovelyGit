using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser;

namespace ExpressThat.LovelyGit.Services.Git.CommitGraph;

public sealed partial class CommitGraphManager
{
    private async Task<CommitGraphTraversalSession> OpenTraversalSessionAsync(
        CommitGraphCursorState cursor,
        CancellationToken cancellationToken)
    {
        if (cursor.RepositoryId == _repositoryId
            && _session != null
            && _session.Offset <= cursor.Offset)
        {
            if (_session.Offset < cursor.Offset)
            {
                await ReadRowsAsync(_session, cursor.Offset - _session.Offset, false, cancellationToken)
                    .ConfigureAwait(false);
            }

            return _session;
        }

        _session = await CreateTraversalSessionAsync(cancellationToken).ConfigureAwait(false);
        if (cursor.RepositoryId == _repositoryId && cursor.Offset > 0)
        {
            await ReadRowsAsync(_session, cursor.Offset, false, cancellationToken)
                .ConfigureAwait(false);
        }

        return _session;
    }

    private async Task<CommitGraphTraversalSession> CreateTraversalSessionAsync(CancellationToken cancellationToken)
    {
        var created = new CommitGraphTraversalSession(_repositoryId);
        foreach (var head in await GetStartingCommitsAsync(cancellationToken).ConfigureAwait(false))
        {
            if (created.MarkSeen(head.Hash))
            {
                created.EnqueueTipFrontier(head.Hash, CommitGraphCommitPriority.FromCommit(head, IsStashRef(head)));
            }
        }

        created.SaveState(0, new List<GitObjectId?>(), new List<int>(), 0);
        return created;
    }
}
