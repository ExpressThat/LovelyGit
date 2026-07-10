using ExpressThat.LovelyGit.Services.Git.Rebase;
using ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.Rebase;

namespace LovelyGit.Tests.Git.Rebase;

public sealed class InteractiveRebasePlanValidatorTests
{
    private static readonly InteractiveRebasePlanResponse Current = new()
    {
        Commits = [Commit("a"), Commit("b")],
    };

    [Fact]
    public void Validate_AllowsReorderingTheCurrentCommitSet()
    {
        InteractiveRebasePlanValidator.Validate(
            Current,
            [Item("b", InteractiveRebaseAction.Pick), Item("a", InteractiveRebaseAction.Reword, "new")]);
    }

    [Fact]
    public void Validate_RejectsStaleDuplicateOrUnknownCommitSets()
    {
        Assert.Throws<InvalidOperationException>(() =>
            InteractiveRebasePlanValidator.Validate(Current, [Item("a", InteractiveRebaseAction.Pick)]));
        Assert.Throws<InvalidOperationException>(() => InteractiveRebasePlanValidator.Validate(
            Current, [Item("a", InteractiveRebaseAction.Pick), Item("a", InteractiveRebaseAction.Pick)]));
        Assert.Throws<InvalidOperationException>(() => InteractiveRebasePlanValidator.Validate(
            Current, [Item("a", InteractiveRebaseAction.Pick), Item("c", InteractiveRebaseAction.Pick)]));
    }

    [Fact]
    public void Validate_RejectsInvalidActionRelationships()
    {
        Assert.Throws<ArgumentException>(() => InteractiveRebasePlanValidator.Validate(
            Current, [Item("a", InteractiveRebaseAction.Drop), Item("b", InteractiveRebaseAction.Fixup)]));
        Assert.Throws<ArgumentException>(() => InteractiveRebasePlanValidator.Validate(
            Current, [Item("a", InteractiveRebaseAction.Reword, " "), Item("b", InteractiveRebaseAction.Pick)]));
        Assert.Throws<ArgumentException>(() => InteractiveRebasePlanValidator.Validate(
            Current, [Item("a", InteractiveRebaseAction.Drop), Item("b", InteractiveRebaseAction.Drop)]));
    }

    private static InteractiveRebaseCommit Commit(string hash) => new() { Hash = hash };

    private static InteractiveRebasePlanItem Item(
        string hash,
        InteractiveRebaseAction action,
        string? message = null) => new() { Hash = hash, Action = action, Message = message };
}
