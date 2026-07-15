using System.Text;
using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser;
using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser.Refs;

namespace LovelyGit.Tests.Git.LovelyFastGitParser;

public sealed class GitStashReaderTests
{
    [Fact]
    public async Task ReadAsync_ReturnsNewestValidEntriesWithStableSelectors()
    {
        using var fixture = new ReflogFixture();
        var longMessage = new string('x', 20_000);
        await fixture.WriteAsync(
            Line(Sha1A, Sha1B, 100, "oldest") + "\r\n" +
            "malformed entry\r\n" +
            Line(Sha1B, Sha1A, 200, longMessage));

        var entries = await GitStashReader.ReadAsync(
            fixture.Path, GitObjectFormat.Sha1, CancellationToken.None);

        Assert.Collection(
            entries,
            newest =>
            {
                Assert.Equal("stash@{0}", newest.Selector);
                Assert.Equal(longMessage, newest.Message);
                Assert.Equal(200, newest.CreatedAtUnixSeconds);
            },
            oldest =>
            {
                Assert.Equal("stash@{1}", oldest.Selector);
                Assert.Equal("oldest", oldest.Message);
                Assert.Equal(100, oldest.CreatedAtUnixSeconds);
            });
    }

    [Fact]
    public async Task ReadAsync_ParsesSha256ObjectIds()
    {
        using var fixture = new ReflogFixture();
        var hashA = new string('a', 64);
        var hashB = new string('b', 64);
        await fixture.WriteAsync(Line(hashA, hashB, 300, "sha256"));

        var entry = Assert.Single(await GitStashReader.ReadAsync(
            fixture.Path, GitObjectFormat.Sha256, CancellationToken.None));

        Assert.Equal(hashB, entry.Target.ToString());
        Assert.Equal("sha256", entry.Message);
    }

    [Fact]
    public async Task ReadAsync_WhenCancelled_DoesNotReturnPartialEntries()
    {
        using var fixture = new ReflogFixture();
        await fixture.WriteAsync(Line(Sha1A, Sha1B, 100, "entry"));
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => GitStashReader.ReadAsync(
            fixture.Path, GitObjectFormat.Sha1, cancellation.Token));
    }

    private static string Line(string oldHash, string newHash, long timestamp, string message) =>
        $"{oldHash} {newHash} User <user@example.invalid> {timestamp} +0000\t{message}";

    private const string Sha1A = "1111111111111111111111111111111111111111";
    private const string Sha1B = "2222222222222222222222222222222222222222";

    private sealed class ReflogFixture : IDisposable
    {
        private readonly DirectoryInfo _directory =
            Directory.CreateTempSubdirectory("lovelygit-stash-reader-");

        public string Path => _directory.FullName;

        public async Task WriteAsync(string contents)
        {
            var refs = Directory.CreateDirectory(System.IO.Path.Combine(Path, "logs", "refs"));
            await File.WriteAllTextAsync(
                System.IO.Path.Combine(refs.FullName, "stash"),
                contents,
                new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
        }

        public void Dispose() => _directory.Delete(recursive: true);
    }
}
