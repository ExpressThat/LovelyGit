using ExpressThat.LovelyGit.Services.Git.Stashes;

namespace LovelyGit.Tests.Git.Stashes;

public sealed class GitStashArgumentTests
{
    [Theory]
    [InlineData("apply", true)]
    [InlineData("pop", true)]
    [InlineData("drop", false)]
    public void ExistingMutationArguments_SuppressDiscardedGitOutput(
        string operation,
        bool restoreIndex)
    {
        var arguments = GitStashCommandService.BuildExistingArguments(
            operation,
            " stash@{7} ",
            restoreIndex,
            quiet: true);

        var expected = restoreIndex
            ? new[] { "stash", operation, "--quiet", "--index", "stash@{7}" }
            : ["stash", operation, "--quiet", "stash@{7}"];
        Assert.Equal(expected, arguments);
    }

    [Fact]
    public void BranchArguments_RetainSupportedSyntax()
    {
        var arguments = GitStashCommandService.BuildExistingArguments(
            "branch",
            "stash@{0}",
            restoreIndex: false,
            quiet: false);

        Assert.Equal(["stash", "branch", "stash@{0}"], arguments);
    }
}
