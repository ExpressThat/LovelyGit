namespace ExpressThat.LovelyGit.Services.Git.Lfs;

internal static class LfsMutationResultValidator
{
    public static void EnsureApplied(
        GitLfsAction action,
        string? pattern,
        LfsRepositoryState state)
    {
        if (action is not (GitLfsAction.Track or GitLfsAction.Untrack)) return;

        var expectedPattern = pattern?.Trim();
        if (string.IsNullOrEmpty(expectedPattern)) return;

        var isTracked = state.TrackedPatterns.Contains(
            expectedPattern,
            StringComparer.Ordinal);
        if (action == GitLfsAction.Track && !isTracked)
        {
            throw new InvalidOperationException(
                "Git LFS did not add the requested pattern to .gitattributes.");
        }

        if (action == GitLfsAction.Untrack && isTracked)
        {
            throw new InvalidOperationException(
                "Git LFS did not remove the requested pattern from .gitattributes.");
        }
    }
}
