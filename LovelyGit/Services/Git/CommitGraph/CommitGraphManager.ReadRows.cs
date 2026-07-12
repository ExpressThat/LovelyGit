using ExpressThat.LovelyGit.Services.Git.CommitGraph.Models;

namespace ExpressThat.LovelyGit.Services.Git.CommitGraph;

public sealed partial class CommitGraphManager
{
    private async Task<CommitGraphResponse> ReadRowsAsync(
        CommitGraphTraversalSession session,
        int limit,
        bool collectRows,
        CancellationToken cancellationToken)
    {
        var offset = session.Offset;
        var activeLaneTargets = session.ActiveLaneTargets;
        var activeLaneColors = session.ActiveLaneColors;
        var maxLaneCount = session.MaxLaneCount;
        var rows = collectRows
            ? limit == int.MaxValue ? new List<CommitGraphRow>() : new List<CommitGraphRow>(limit)
            : CommitGraphEmptyLists.Rows;
        var rowCount = 0;

        while (rowCount < limit && TryGetNextCommit(session, out var nextCommit))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var commit = collectRows
                ? await TryGetCommitAsync(nextCommit, cancellationToken).ConfigureAwait(false)
                : await TryGetCommitHeaderAsync(nextCommit, cancellationToken).ConfigureAwait(false);
            if (commit == null)
            {
                continue;
            }

            var rowIndex = offset + rowCount;
            session.MarkProcessed(commit.Hash);
            var parents = GetGraphParents(commit, session.ProcessedHashes);
            await EnqueueParentsAsync(session, parents, collectRows, cancellationToken).ConfigureAwait(false);

            if (collectRows)
            {
                rows.Add(CommitGraphRowBuilder.Build(
                    commit,
                    parents,
                    rowIndex,
                    activeLaneTargets,
                    activeLaneColors,
                    session.AllocateColor,
                    ref maxLaneCount,
                    _remoteUrl));
            }
            else
            {
                CommitGraphRowBuilder.AdvanceLanes(
                    commit,
                    parents,
                    activeLaneTargets,
                    activeLaneColors,
                    session.AllocateColor,
                    ref maxLaneCount);
            }

            rowCount++;
        }

        var nextOffset = offset + rowCount;
        var hasMore = session.PendingCount > 0;
        session.SaveState(nextOffset, activeLaneTargets, activeLaneColors, maxLaneCount);
        return new CommitGraphResponse
        {
            TotalRows = hasMore ? nextOffset + limit : nextOffset,
            LaneCount = maxLaneCount,
            Rows = rows,
            RemotePrefixes = _repository.RemotePrefixes.ToList(),
            RemoteRepositoryUrl = RemoteCommitUrlBuilder.BuildRepository(_remoteUrl),
            CurrentBranchName = _repository.CurrentBranchName,
            HasMore = hasMore,
        };
    }

    private async Task EnqueueParentsAsync(
        CommitGraphTraversalSession session,
        CommitGraphParentSet parents,
        bool collectRows,
        CancellationToken cancellationToken)
    {
        for (var index = 0; index < parents.Count; index++)
        {
            var parentId = parents[index];
            if (!session.MarkSeen(parentId))
            {
                continue;
            }

            var parent = collectRows
                ? await TryGetCommitAsync(parentId, cancellationToken).ConfigureAwait(false)
                : await TryGetCommitHeaderAsync(parentId, cancellationToken).ConfigureAwait(false);
            if (parent != null)
            {
                session.EnqueueActiveFrontier(parentId, CommitGraphCommitPriority.FromCommit(parent));
            }
        }
    }
}
