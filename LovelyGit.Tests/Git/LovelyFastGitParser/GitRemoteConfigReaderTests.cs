using System.Text;
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
                pushurl = git@github.com:example/origin.git
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
            [remote "backup"]
                url = https://example.invalid/backup.git
            """);

        var url = await GitRemoteConfigReader.ReadPrimaryRemoteUrlAsync(
            directory.Path,
            CancellationToken.None);

        Assert.Equal("git@gitlab.com:example/upstream.git", url);
    }

    [Fact]
    public async Task ReadPrimaryRemoteUrlAsync_PreservesDuplicateAndFallbackSemantics()
    {
        using var directory = TemporaryGitDirectory.Create(
            "[remote \"first\"]\r\n\turl = https://example.invalid/first.git\r\n" +
            "[remote \"second\"]\r\n\turl = https://example.invalid/second.git\r\n" +
            "[remote \"origin\"]\r\n\turl = https://example.invalid/old.git\r\n" +
            $"[core]\r\n\tcomment = {new string('x', 20_000)}\r\n" +
            "[remote \"origin\"]\r\n\turl = \"https://example.invalid/new.git\"",
            new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));

        var url = await GitRemoteConfigReader.ReadPrimaryRemoteUrlAsync(
            directory.Path,
            CancellationToken.None);

        Assert.Equal("https://example.invalid/new.git", url);
    }

    [Fact]
    public async Task ReadPrimaryRemoteUrlAsync_WhenCancelled_DoesNotChangeConfig()
    {
        using var directory = TemporaryGitDirectory.Create(
            "[remote \"origin\"]\n\turl = https://example.invalid/origin.git\n");
        var path = Path.Combine(directory.Path, "config");
        var before = await File.ReadAllBytesAsync(path);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            GitRemoteConfigReader.ReadPrimaryRemoteUrlAsync(directory.Path, cancellation.Token));

        Assert.Equal(before, await File.ReadAllBytesAsync(path));
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
                pushurl = git@github.com:example/origin.git
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
                Assert.Equal("git@github.com:example/origin.git", remote.PushUrl);
            },
            remote =>
            {
                Assert.Equal("upstream", remote.Name);
                Assert.Equal("git@gitlab.com:example/upstream.git", remote.Url);
                Assert.Null(remote.PushUrl);
            });
    }

    [Fact]
    public async Task ReadRemotesAsync_DoesNotReadUrlsFromLaterSections()
    {
        using var directory = TemporaryGitDirectory.Create(
            """
            [remote "origin"]
                url = https://github.com/example/origin.git
            [credential]
                url = https://credentials.example.invalid/not-a-remote.git
            """);

        var remote = Assert.Single(await GitRemoteConfigReader.ReadRemotesAsync(
            directory.Path,
            CancellationToken.None));

        Assert.Equal("https://github.com/example/origin.git", remote.Url);
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

        public static TemporaryGitDirectory Create(string configText, Encoding? encoding = null)
        {
            var directory = Directory.CreateTempSubdirectory("lovelygit-remote-config-");
            File.WriteAllText(
                System.IO.Path.Combine(directory.FullName, "config"),
                configText,
                encoding ?? new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
            return new TemporaryGitDirectory(directory);
        }

        public void Dispose()
        {
            _directory.Delete(recursive: true);
        }
    }
}
