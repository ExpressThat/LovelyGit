using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser.Remotes;

namespace LovelyGit.Tests.Git.LovelyFastGitParser;

public sealed class GitRemoteConfigReaderTests
{
    [Fact]
    public async Task ReadPrimaryRemoteUrlAsync_PrefersOriginRemote()
    {
        using var directory = TemporaryGitDirectory.Create(
            """
            [remote "upstream"]
                url = https://github.com/example/upstream.git
            [remote "origin"]
                url = https://github.com/example/origin.git
            """);

        var url = await GitRemoteConfigReader.ReadPrimaryRemoteUrlAsync(
            directory.Path,
            CancellationToken.None);

        Assert.Equal("https://github.com/example/origin.git", url);
    }

    [Fact]
    public async Task ReadPrimaryRemoteUrlAsync_FallsBackToFirstRemote()
    {
        using var directory = TemporaryGitDirectory.Create(
            """
            [remote "upstream"]
                url = git@gitlab.com:example/upstream.git
            """);

        var url = await GitRemoteConfigReader.ReadPrimaryRemoteUrlAsync(
            directory.Path,
            CancellationToken.None);

        Assert.Equal("git@gitlab.com:example/upstream.git", url);
    }

    [Fact]
    public async Task ReadRemotesAsync_ReturnsConfiguredRemotesByName()
    {
        using var directory = TemporaryGitDirectory.Create(
            """
            [remote "upstream"]
                url = git@gitlab.com:example/upstream.git
            [remote "origin"]
                url = https://github.com/example/origin.git
            """);

        var remotes = await GitRemoteConfigReader.ReadRemotesAsync(
            directory.Path,
            CancellationToken.None);

        Assert.Collection(
            remotes,
            remote =>
            {
                Assert.Equal("origin", remote.Name);
                Assert.Equal("https://github.com/example/origin.git", remote.Url);
            },
            remote =>
            {
                Assert.Equal("upstream", remote.Name);
                Assert.Equal("git@gitlab.com:example/upstream.git", remote.Url);
            });
    }

    private sealed class TemporaryGitDirectory : IDisposable
    {
        private readonly DirectoryInfo _directory;

        private TemporaryGitDirectory(DirectoryInfo directory)
        {
            _directory = directory;
            Path = directory.FullName;
        }

        public string Path { get; }

        public static TemporaryGitDirectory Create(string configText)
        {
            var directory = Directory.CreateTempSubdirectory("lovelygit-remote-config-");
            File.WriteAllText(System.IO.Path.Combine(directory.FullName, "config"), configText);
            return new TemporaryGitDirectory(directory);
        }

        public void Dispose()
        {
            _directory.Delete(recursive: true);
        }
    }
}
