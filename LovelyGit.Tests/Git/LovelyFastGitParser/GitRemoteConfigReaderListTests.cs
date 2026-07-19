using System.Text;
using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser.Remotes;

namespace LovelyGit.Tests.Git.LovelyFastGitParser;

public sealed class GitRemoteConfigReaderListTests
{
    [Fact]
    public async Task ReadRemotesAsync_PreservesDuplicateSectionsAndLongIgnoredLines()
    {
        using var directory = TemporaryConfig.Create(
            "[remote \"origin\"]\r\n" +
            "\turl = https://example.invalid/old.git\r\n" +
            $"\tfetch = {new string('x', 20_000)}\r\n" +
            "[remote \"origin\"]\r\n" +
            "\tpushurl = ssh://example.invalid/origin.git\r\n" +
            "\turl = https://example.invalid/new.git");

        var remote = Assert.Single(await GitRemoteConfigReader.ReadRemotesAsync(
            directory.Path, CancellationToken.None));

        Assert.Equal("https://example.invalid/new.git", remote.Url);
        Assert.Equal("ssh://example.invalid/origin.git", remote.PushUrl);
    }

    [Fact]
    public async Task ReadRemotesAsync_WhenCancelled_DoesNotChangeConfig()
    {
        using var directory = TemporaryConfig.Create(
            "[remote \"origin\"]\n\turl = https://example.invalid/origin.git\n");
        var path = System.IO.Path.Combine(directory.Path, "config");
        var before = await File.ReadAllBytesAsync(path);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            GitRemoteConfigReader.ReadRemotesAsync(directory.Path, cancellation.Token));

        Assert.Equal(before, await File.ReadAllBytesAsync(path));
    }

    private sealed class TemporaryConfig : IDisposable
    {
        private readonly DirectoryInfo _directory;

        private TemporaryConfig(DirectoryInfo directory)
        {
            _directory = directory;
            Path = directory.FullName;
        }

        public string Path { get; }

        public static TemporaryConfig Create(string text)
        {
            var directory = Directory.CreateTempSubdirectory("lovelygit-remote-list-");
            File.WriteAllText(
                System.IO.Path.Combine(directory.FullName, "config"),
                text,
                new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
            return new TemporaryConfig(directory);
        }

        public void Dispose() => _directory.Delete(recursive: true);
    }
}
