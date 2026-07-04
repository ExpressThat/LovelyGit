using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser;

namespace ExpressThat.LovelyGit.Services.Git.CommitGraph;

internal readonly record struct CommitGraphParentSet(
    GitCommit Commit,
    bool StashMainOnly,
    IReadOnlySet<GitObjectId>? ProcessedParents = null)
{
    public int Count => StashMainOnly ? 1 : Commit.ParentHashCount;

    public GitObjectId this[int index] => Commit.GetParentHash(index);

    public bool IsProcessed(int index)
    {
        return ProcessedParents?.Contains(this[index]) == true;
    }
}
