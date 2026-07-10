using ExpressThat.LovelyGit.Services.Git.Branches;

namespace LovelyGit.Tests.Git.Branches;

public sealed class GitBranchUpstreamConfigReaderTests
{
    [Fact]
    public async Task ReadAsync_ReturnsRemoteAndLocalUpstreamsInBranchOrder()
    {
        using var directory = new TemporaryDirectory();
        await File.WriteAllTextAsync(
            Path.Combine(directory.Path, "config"),
            """
            [remote "origin"]
                url = https://example.invalid/repository.git
            [branch "zebra"]
                remote = .
                merge = refs/heads/local-base
            [branch "main"]
                remote = origin
                merge = refs/heads/main
            """);

        var upstreams = await GitBranchUpstreamConfigReader.ReadAsync(
            directory.Path,
            CancellationToken.None);

        Assert.Collection(
            upstreams,
            upstream => Assert.Equal(new("main", "origin/main"), upstream),
            upstream => Assert.Equal(new("zebra", "local-base"), upstream));
    }

    [Fact]
    public async Task ReadAsync_IgnoresIncompleteAndNonBranchSections()
    {
        using var directory = new TemporaryDirectory();
        await File.WriteAllTextAsync(
            Path.Combine(directory.Path, "config"),
            """
            [branch "missing-merge"]
                remote = origin
            [branch "tag-merge"]
                remote = origin
                merge = refs/tags/v1
            [core]
                remote = ignored
                merge = refs/heads/ignored
            """);

        var upstreams = await GitBranchUpstreamConfigReader.ReadAsync(
            directory.Path,
            CancellationToken.None);

        Assert.Empty(upstreams);
    }

    [Fact]
    public async Task ReadAsync_ReturnsEmptyWhenConfigDoesNotExist()
    {
        using var directory = new TemporaryDirectory();

        var upstreams = await GitBranchUpstreamConfigReader.ReadAsync(
            directory.Path,
            CancellationToken.None);

        Assert.Empty(upstreams);
    }

    private sealed class TemporaryDirectory : IDisposable
    {
        public TemporaryDirectory()
        {
            Path = Directory.CreateTempSubdirectory("lovelygit-upstreams-").FullName;
        }

        public string Path { get; }

        public void Dispose() => Directory.Delete(Path, recursive: true);
    }
}
