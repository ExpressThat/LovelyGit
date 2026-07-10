using ExpressThat.LovelyGit.Services.Git.Rebase;

namespace LovelyGit.Tests.Git.Rebase;

public sealed class GitShellArgumentTests
{
    [Theory]
    [InlineData("simple", "'simple'")]
    [InlineData("path with spaces/$value", "'path with spaces/$value'")]
    [InlineData("Ross's repo", "'Ross'\"'\"'s repo'")]
    public void Quote_ProducesSingleShellArgument(string value, string expected)
    {
        Assert.Equal(expected, GitShellArgument.Quote(value));
    }
}
