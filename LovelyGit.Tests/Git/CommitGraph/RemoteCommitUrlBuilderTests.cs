using ExpressThat.LovelyGit.Services.Git.CommitGraph;

namespace LovelyGit.Tests.Git.CommitGraph;

public sealed class RemoteCommitUrlBuilderTests
{
    [Theory]
    [InlineData("https://github.com/example/repo.git", "https://github.com/example/repo/commit/abc123")]
    [InlineData("git@github.com:example/repo.git", "https://github.com/example/repo/commit/abc123")]
    [InlineData("ssh://git@gitlab.com/example/repo.git", "https://gitlab.com/example/repo/-/commit/abc123")]
    [InlineData("https://bitbucket.org/example/repo", "https://bitbucket.org/example/repo/commits/abc123")]
    [InlineData("https://dev.azure.com/org/project/_git/repo", "https://dev.azure.com/org/project/_git/repo/commit/abc123")]
    public void Build_ReturnsProviderCommitUrl(string remoteUrl, string expected)
    {
        var result = RemoteCommitUrlBuilder.Build(remoteUrl, "abc123");

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("https://github.com/example/repo.git", "https://github.com/example/repo")]
    [InlineData("git@github.com:example/repo.git", "https://github.com/example/repo")]
    [InlineData("ssh://git@gitlab.com/example/repo.git", "https://gitlab.com/example/repo")]
    public void BuildRepository_ReturnsProviderRepositoryUrl(string remoteUrl, string expected)
    {
        var result = RemoteCommitUrlBuilder.BuildRepository(remoteUrl);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("https://github.com/example/repo.git", "v1.0.0", "https://github.com/example/repo/releases/tag/v1.0.0")]
    [InlineData("ssh://git@gitlab.com/example/repo.git", "release/test", "https://gitlab.com/example/repo/-/tags/release%2Ftest")]
    [InlineData("https://bitbucket.org/example/repo", "v2", "https://bitbucket.org/example/repo/src/v2")]
    [InlineData("https://dev.azure.com/org/project/_git/repo", "v3", "https://dev.azure.com/org/project/_git/repo/tree/v3")]
    public void BuildTag_ReturnsProviderTagUrl(
        string remoteUrl,
        string tagName,
        string expected)
    {
        var result = RemoteCommitUrlBuilder.BuildTag(remoteUrl, tagName);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-a-web-url")]
    [InlineData("ftp://example/repo")]
    public void Build_ReturnsNullForUnsupportedRemoteUrl(string remoteUrl)
    {
        Assert.Null(RemoteCommitUrlBuilder.Build(remoteUrl, "abc123"));
    }
}
