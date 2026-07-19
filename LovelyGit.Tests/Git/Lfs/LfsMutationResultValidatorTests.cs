using ExpressThat.LovelyGit.Services.Git.Lfs;

namespace LovelyGit.Tests.Git.Lfs;

public sealed class LfsMutationResultValidatorTests
{
    [Fact]
    public void Track_RejectsAZeroExitThatDidNotAddThePattern()
    {
        var state = State("existing/**");

        var exception = Assert.Throws<InvalidOperationException>(() =>
            LfsMutationResultValidator.EnsureApplied(
                GitLfsAction.Track,
                "missing/**",
                state));

        Assert.Contains("did not add", exception.Message);
        Assert.Equal(["existing/**"], state.TrackedPatterns);
    }

    [Fact]
    public void Untrack_RejectsAZeroExitThatDidNotRemoveThePattern()
    {
        var state = State("still-tracked/**");

        var exception = Assert.Throws<InvalidOperationException>(() =>
            LfsMutationResultValidator.EnsureApplied(
                GitLfsAction.Untrack,
                "still-tracked/**",
                state));

        Assert.Contains("did not remove", exception.Message);
        Assert.Equal(["still-tracked/**"], state.TrackedPatterns);
    }

    [Theory]
    [InlineData(GitLfsAction.Track, "tracked/**", "tracked/**")]
    [InlineData(GitLfsAction.Untrack, "removed/**", "other/**")]
    [InlineData(GitLfsAction.Prune, null, "tracked/**")]
    public void AppliedOrUnrelatedActions_Succeed(
        GitLfsAction action,
        string? pattern,
        string remainingPattern)
    {
        var state = State(remainingPattern);

        LfsMutationResultValidator.EnsureApplied(action, pattern, state);

        Assert.Equal([remainingPattern], state.TrackedPatterns);
    }

    private static LfsRepositoryState State(params string[] patterns) => new()
    {
        HasTrackedPatterns = patterns.Length > 0,
        TrackedPatterns = [.. patterns],
    };
}
