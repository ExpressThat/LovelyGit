using ExpressThat.LovelyGit.Services.Git.Cli;

namespace LovelyGit.Tests.Git.Cli;

public sealed class GitRemoteNameValidatorTests
{
    [Theory]
    [InlineData("origin")]
    [InlineData("upstream")]
    [InlineData("lovelygit-visual")]
    public void IsValidRemoteName_AcceptsCommonRemoteNames(string remoteName)
    {
        Assert.True(GitRemoteNameValidator.IsValidRemoteName(remoteName));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("-origin")]
    [InlineData("origin/main")]
    [InlineData("origin main")]
    [InlineData("origin:main")]
    public void IsValidRemoteName_RejectsUnsafeRemoteNames(string remoteName)
    {
        Assert.False(GitRemoteNameValidator.IsValidRemoteName(remoteName));
    }
}
