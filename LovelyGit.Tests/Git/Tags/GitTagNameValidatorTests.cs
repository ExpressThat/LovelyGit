using ExpressThat.LovelyGit.Services.Git.Tags;

namespace LovelyGit.Tests.Git.Tags;

public sealed class GitTagNameValidatorTests
{
    [Theory]
    [InlineData("v1.0.0")]
    [InlineData("release/2026.06")]
    [InlineData("build-123")]
    [InlineData("user.name/tag")]
    public void IsValidTagName_AcceptsValidNames(string tagName)
    {
        Assert.True(GitTagNameValidator.IsValidTagName(tagName));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("@")]
    [InlineData("-bad")]
    [InlineData("/v1")]
    [InlineData("v1/")]
    [InlineData("refs/tags/v1")]
    [InlineData("v1//tag")]
    [InlineData("v1..tag")]
    [InlineData("v1.lock")]
    [InlineData("v1 tag")]
    [InlineData("v1~tag")]
    [InlineData("v1^tag")]
    [InlineData("v1:tag")]
    [InlineData("v1?tag")]
    [InlineData("v1*tag")]
    [InlineData("v1[tag")]
    [InlineData(@"v1\tag")]
    [InlineData("v1@{tag")]
    [InlineData("v1.")]
    public void IsValidTagName_RejectsInvalidNames(string tagName)
    {
        Assert.False(GitTagNameValidator.IsValidTagName(tagName));
    }
}
