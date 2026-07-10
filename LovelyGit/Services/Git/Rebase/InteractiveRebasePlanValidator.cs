using ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.Rebase;

namespace ExpressThat.LovelyGit.Services.Git.Rebase;

internal static class InteractiveRebasePlanValidator
{
    public static void Validate(
        InteractiveRebasePlanResponse current,
        IReadOnlyList<InteractiveRebasePlanItem> requested)
    {
        if (requested.Count != current.Commits.Count || requested.Count == 0)
        {
            throw new InvalidOperationException("The branch changed after this rebase plan was opened.");
        }

        var expected = current.Commits.Select(commit => commit.Hash).ToHashSet(StringComparer.Ordinal);
        var actual = requested.Select(item => item.Hash).ToHashSet(StringComparer.Ordinal);
        if (actual.Count != requested.Count || !expected.SetEquals(actual))
        {
            throw new InvalidOperationException("The branch changed after this rebase plan was opened.");
        }

        var hasRetainedCommit = false;
        foreach (var item in requested)
        {
            if (item.Action is InteractiveRebaseAction.Squash or InteractiveRebaseAction.Fixup &&
                !hasRetainedCommit)
            {
                throw new ArgumentException("Squash and fixup require a preceding retained commit.");
            }

            if (item.Action == InteractiveRebaseAction.Reword && string.IsNullOrWhiteSpace(item.Message))
            {
                throw new ArgumentException("Every reworded commit needs a commit message.");
            }

            if (item.Message?.Length > 100_000)
            {
                throw new ArgumentException("A commit message cannot exceed 100,000 characters.");
            }

            hasRetainedCommit |= item.Action != InteractiveRebaseAction.Drop;
        }

        if (!hasRetainedCommit)
        {
            throw new ArgumentException("Keep at least one commit in the rebase plan.");
        }
    }
}
