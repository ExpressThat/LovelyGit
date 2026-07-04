using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser.Refs;

namespace ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser;

internal sealed partial class LovelyGitRepository
{
    public async Task<IReadOnlyList<GitCommit>> GetStartingCommitsAsync(CancellationToken cancellationToken)
    {
        var orderedIds = GetOrderedStartingCommitIds();
        return await ResolveStartingCommitsAsync(orderedIds, cancellationToken).ConfigureAwait(false);
    }

    private List<GitObjectId> GetOrderedStartingCommitIds()
    {
        var seenIds = new HashSet<GitObjectId>();
        var orderedIds = new List<GitObjectId>();
        if (HeadTarget != null && seenIds.Add(HeadTarget.Value))
        {
            orderedIds.Add(HeadTarget.Value);
        }

        foreach (var reference in _refsByFullName.Values
                     .Where(reference => reference.Kind != GitRefKind.Tag)
                     .OrderBy(reference => reference.Kind == GitRefKind.Stash ? 0 : 1))
        {
            if (seenIds.Add(reference.Target))
            {
                orderedIds.Add(reference.Target);
            }
        }

        return orderedIds;
    }

    private async Task<IReadOnlyList<GitCommit>> ResolveStartingCommitsAsync(
        IReadOnlyList<GitObjectId> orderedIds,
        CancellationToken cancellationToken)
    {
        var commits = new GitCommit?[orderedIds.Count];
        await Parallel.ForEachAsync(
                Enumerable.Range(0, orderedIds.Count),
                cancellationToken,
                async (index, itemCancellationToken) =>
                {
                    itemCancellationToken.ThrowIfCancellationRequested();
                    try
                    {
                        commits[index] = await GetGraphCommitAsync(orderedIds[index], itemCancellationToken)
                            .ConfigureAwait(false);
                    }
                    catch when (!itemCancellationToken.IsCancellationRequested)
                    {
                    }
                })
            .ConfigureAwait(false);

        return commits.Where(commit => commit != null).Select(commit => commit!).ToList();
    }

}
