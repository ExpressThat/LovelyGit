using System.IO.Compression;
using System.Text.Json;
using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser.Remotes;
using ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.WorkingTree;

namespace LovelyGit.Tests.Git.Remotes;

public sealed class RemotePayloadCompactorTests
{
    [Fact]
    public void CompactIfUseful_LeavesSmallRemoteListUncompressed()
    {
        var remotes = Remotes(RemotePayloadCompactor.CompressionThreshold - 1);

        var response = RemotePayloadCompactor.CompactIfUseful(remotes);

        Assert.Same(remotes, response.Remotes);
        Assert.Null(response.CompactRemotesGzipBase64);
    }

    [Fact]
    public void CompactIfUseful_RoundTripsEveryLargeRemote()
    {
        var remotes = Remotes(10_000);

        var response = RemotePayloadCompactor.CompactIfUseful(remotes);
        var bytes = Convert.FromBase64String(response.CompactRemotesGzipBase64!);
        using var input = new MemoryStream(bytes);
        using var gzip = new GZipStream(input, CompressionMode.Decompress);
        var restored = JsonSerializer.Deserialize(
            gzip,
            WorkingTreeJsonSerializerContext.Default.ListGitRemote);

        Assert.Empty(response.Remotes);
        Assert.Equal(remotes, restored);
        Assert.True(response.CompactRemotesGzipBase64!.Length < 200_000);
    }

    [Fact]
    public void CompactIfUseful_CompactsFewUnusuallyLongUrls()
    {
        var remotes = new List<GitRemote>
        {
            new()
            {
                Name = "origin",
                Url = $"https://example.invalid/{new string('x', 128_000)}",
            },
        };

        Assert.NotNull(RemotePayloadCompactor
            .CompactIfUseful(remotes).CompactRemotesGzipBase64);
    }

    private static List<GitRemote> Remotes(int count) =>
        Enumerable.Range(0, count)
            .Select(index => new GitRemote
            {
                Name = $"remote-{index:D5}",
                Url = $"https://example.invalid/org/remote-{index:D5}.git",
                PushUrl = index % 2 == 0
                    ? $"ssh://git@example.invalid/org/remote-{index:D5}.git"
                    : null,
            })
            .ToList();
}
