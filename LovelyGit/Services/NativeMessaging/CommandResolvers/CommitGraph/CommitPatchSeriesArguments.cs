using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser;

namespace ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.CommitGraph;

internal static class CommitPatchSeriesArguments
{
    private const int MaximumCommits = 50;

    public static bool TryParse(
        CommitPatchSeriesCommandArguments? arguments,
        out List<GitObjectId> ids,
        out string error)
    {
        ids = [];
        if (arguments == null || arguments.RepositoryId == Guid.Empty)
        {
            error = "RepositoryId is required.";
            return false;
        }

        if (arguments.CommitHashes.Count is < 1 or > MaximumCommits)
        {
            error = $"CommitHashes must contain between 1 and {MaximumCommits} commits.";
            return false;
        }

        var unique = new HashSet<GitObjectId>();
        foreach (var hash in arguments.CommitHashes)
        {
            if (!GitObjectId.TryParse(hash, out var id))
            {
                error = "CommitHashes contains an invalid commit hash.";
                return false;
            }

            if (!unique.Add(id))
            {
                error = "CommitHashes cannot contain duplicates.";
                return false;
            }

            ids.Add(id);
        }

        error = string.Empty;
        return true;
    }
}
