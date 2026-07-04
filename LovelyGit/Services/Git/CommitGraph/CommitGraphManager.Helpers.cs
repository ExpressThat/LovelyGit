using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser;

namespace ExpressThat.LovelyGit.Services.Git.CommitGraph;

public sealed partial class CommitGraphManager
{
    private async Task<IReadOnlyList<GitCommit>> GetStartingCommitsAsync(CancellationToken cancellationToken)
    {
        return await _repository.GetStartingCommitsAsync(cancellationToken).ConfigureAwait(false);
    }

    private static CommitGraphParentSet GetGraphParents(
        GitCommit commit,
        IReadOnlySet<GitObjectId>? processedParents = null)
    {
        return new CommitGraphParentSet(
            commit,
            IsStashRef(commit) && commit.ParentHashCount > 1,
            processedParents);
    }

    private static bool IsStashRef(GitCommit commit)
    {
        foreach (var reference in commit.Refs)
        {
            if (reference.Kind == GitRefKind.Stash)
            {
                return true;
            }
        }

        return false;
    }

    private async Task<GitCommit?> TryGetCommitAsync(GitObjectId hash, CancellationToken cancellationToken)
    {
        try
        {
            return await _repository.GetGraphCommitAsync(hash, cancellationToken).ConfigureAwait(false);
        }
        catch when (!cancellationToken.IsCancellationRequested)
        {
            return null;
        }
    }

    private async Task<GitCommit?> TryGetCommitHeaderAsync(GitObjectId hash, CancellationToken cancellationToken)
    {
        try
        {
            return await _repository.GetGraphCommitHeaderAsync(hash, cancellationToken).ConfigureAwait(false);
        }
        catch when (!cancellationToken.IsCancellationRequested)
        {
            return null;
        }
    }

    public static CommitGraphCursorState DecodeCursorState(string? cursor)
    {
        return CommitGraphCursor.Decode(cursor);
    }

    public static string EncodeCursorState(CommitGraphCursorState cursor)
    {
        return CommitGraphCursor.Encode(cursor);
    }
}
