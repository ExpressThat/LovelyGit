using ExpressThat.LovelyGit.Services.Git.Branches;

namespace LovelyGit.Tests.Git.Branches;

public sealed class GitBranchNameValidatorTests
{
    [Theory]
    [InlineData("feature/context-menu")]
    [InlineData("bugfix-123")]
    [InlineData("release/v1.0.0")]
    [InlineData("user.name/topic")]
    public void IsValidBranchName_AcceptsValidNames(string branchName)
    {
        Assert.True(GitBranchNameValidator.IsValidBranchName(branchName));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("/feature")]
    [InlineData("feature/")]
    [InlineData("feature//topic")]
    [InlineData("feature..topic")]
    [InlineData("feature.lock")]
    [InlineData("feature topic")]
    [InlineData("feature~topic")]
    [InlineData("feature^topic")]
    [InlineData("feature:topic")]
    [InlineData("feature?topic")]
    [InlineData("feature*topic")]
    [InlineData("feature[topic")]
    [InlineData(@"feature\topic")]
    [InlineData("feature@{topic")]
    [InlineData("feature.")]
    public void IsValidBranchName_RejectsInvalidNames(string branchName)
    {
        Assert.False(GitBranchNameValidator.IsValidBranchName(branchName));
    }
}
