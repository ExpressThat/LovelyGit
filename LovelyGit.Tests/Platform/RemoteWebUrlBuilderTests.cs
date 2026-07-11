using ExpressThat.LovelyGit.Services.Platform;

namespace LovelyGit.Tests.Platform;

public sealed class RemoteWebUrlBuilderTests
{
    [Theory]
    [InlineData("git@github.com:owner/project.git", "https://github.com/owner/project")]
    [InlineData("ssh://git@gitlab.com/owner/project.git", "https://gitlab.com/owner/project")]
    [InlineData("https://bitbucket.org/owner/project.git/", "https://bitbucket.org/owner/project")]
    [InlineData("git@ssh.dev.azure.com:v3/org/project/repo", "https://dev.azure.com/org/project/_git/repo")]
    public void Build_NormalizesRepositoryRemotes(string remote, string expected)
    {
        Assert.Equal(expected, RemoteWebUrlBuilder.Build(remote, RemoteWebResourceKind.Repository, null));
    }

    [Theory]
    [InlineData("https://github.com/o/r.git", "https://github.com/o/r/commit/abcdef")]
    [InlineData("https://gitlab.com/o/r.git", "https://gitlab.com/o/r/-/commit/abcdef")]
    [InlineData("git@bitbucket.org:o/r.git", "https://bitbucket.org/o/r/commits/abcdef")]
    [InlineData("https://dev.azure.com/o/p/_git/r", "https://dev.azure.com/o/p/_git/r/commit/abcdef")]
    public void Build_UsesProviderCommitRoute(string remote, string expected)
    {
        Assert.Equal(expected, RemoteWebUrlBuilder.Build(remote, RemoteWebResourceKind.Commit, "abcdef"));
    }

    [Theory]
    [InlineData("https://github.com/o/r", "https://github.com/o/r/tree/feature/nice-ui")]
    [InlineData("https://gitlab.com/o/r", "https://gitlab.com/o/r/-/tree/feature/nice-ui")]
    [InlineData("https://bitbucket.org/o/r", "https://bitbucket.org/o/r/branch/feature/nice-ui")]
    [InlineData("https://dev.azure.com/o/p/_git/r", "https://dev.azure.com/o/p/_git/r?version=GBfeature%2Fnice-ui")]
    public void Build_PreservesBranchPathSegments(string remote, string expected)
    {
        Assert.Equal(expected, RemoteWebUrlBuilder.Build(remote, RemoteWebResourceKind.Branch, "feature/nice-ui"));
    }

    [Theory]
    [InlineData("https://github.com/o/r", "https://github.com/o/r/compare/main...feature/nice-ui?expand=1")]
    [InlineData("https://gitlab.com/o/r", "https://gitlab.com/o/r/-/merge_requests/new?merge_request%5Bsource_branch%5D=feature%2Fnice-ui&merge_request%5Btarget_branch%5D=main")]
    [InlineData("https://bitbucket.org/o/r", "https://bitbucket.org/o/r/pull-requests/new?source=feature%2Fnice-ui&dest=main")]
    [InlineData("https://dev.azure.com/o/p/_git/r", "https://dev.azure.com/o/p/_git/r/pullrequestcreate?sourceRef=refs%2Fheads%2Ffeature%2Fnice-ui&targetRef=refs%2Fheads%2Fmain")]
    public void Build_UsesProviderPullRequestRoute(string remote, string expected)
    {
        Assert.Equal(
            expected,
            RemoteWebUrlBuilder.Build(remote, RemoteWebResourceKind.PullRequest, "feature/nice-ui", "main"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("C:\\repo")]
    [InlineData("file:///repo")]
    public void Build_RejectsMissingOrUnsafeRemotes(string remote)
    {
        Assert.ThrowsAny<Exception>(() =>
            RemoteWebUrlBuilder.Build(remote, RemoteWebResourceKind.Repository, null));
    }

    [Fact]
    public void Build_RequiresAResourceValue()
    {
        Assert.Throws<ArgumentException>(() =>
            RemoteWebUrlBuilder.Build("https://github.com/o/r", RemoteWebResourceKind.Commit, " "));
    }

    [Fact]
    public void Build_RequiresAPullRequestTarget()
    {
        Assert.Throws<ArgumentException>(() =>
            RemoteWebUrlBuilder.Build("https://github.com/o/r", RemoteWebResourceKind.PullRequest, "feature", " "));
    }

    [Fact]
    public void Launcher_OnlyAllowsHttpsUrls()
    {
        var startInfo = RemoteWebLauncher.CreateStartInfo("https://github.com/o/r");

        Assert.True(startInfo.UseShellExecute);
        Assert.Equal("https://github.com/o/r", startInfo.FileName.TrimEnd('/'));
        Assert.Throws<ArgumentException>(() => RemoteWebLauncher.CreateStartInfo("http://example.com"));
    }

    [Fact]
    public void Launcher_SurfacesStartFailureAndCanBeRetried()
    {
        var attempts = 0;
        var launcher = new RemoteWebLauncher(_ =>
        {
            attempts++;
            return null;
        });

        Assert.Throws<InvalidOperationException>(() => launcher.Open("https://github.com/o/r"));
        Assert.Throws<InvalidOperationException>(() => launcher.Open("https://github.com/o/r"));
        Assert.Equal(2, attempts);
    }
}
