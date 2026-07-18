using System.IO.Compression;
using System.Text.Json;
using ExpressThat.LovelyGit.Services.Git.FileBlame;
using ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.CommitGraph;

namespace LovelyGit.Tests.Git.FileBlame;

public sealed class FileBlamePayloadCompactorTests
{
    [Fact]
    public void CompactIfUseful_LeavesSmallResponsesReadable()
    {
        var response = Response("one\n", []);

        Assert.Same(response, FileBlamePayloadCompactor.CompactIfUseful(response));
    }

    [Fact]
    public void CompactIfUseful_RoundTripsLargeContentAndHunks()
    {
        var content = string.Concat(Enumerable.Repeat("representative source line\n", 50_000));
        var response = Response(content, Enumerable.Range(0, 5_000)
            .Select(index => new FileBlameHunk { StartLine = index + 1, LineCount = 1, Hash = $"{index:x40}" })
            .ToList());

        var compact = FileBlamePayloadCompactor.CompactIfUseful(response);
        var expanded = Expand(compact.CompactPayloadGzipBase64);

        Assert.Empty(compact.Content);
        Assert.Empty(compact.Hunks);
        Assert.Equal(response.Content, expanded.Content);
        Assert.Equal(response.Path, expanded.Path);
        Assert.Equal(response.Hunks.Select(HunkIdentity), expanded.Hunks.Select(HunkIdentity));
        Assert.True(compact.CompactPayloadGzipBase64.Length < 200_000);
    }

    [Fact]
    public void CompactIfUseful_RejectsAnUnsafeIncompressibleEnvelope()
    {
        var random = new byte[700_000];
        new Random(42).NextBytes(random);
        var response = Response(Convert.ToBase64String(random), []);

        var error = Assert.Throws<InvalidDataException>(() =>
            FileBlamePayloadCompactor.CompactIfUseful(response));

        Assert.Contains("could not be compacted", error.Message);
    }

    private static FileBlameResponse Expand(string value)
    {
        using var input = new MemoryStream(Convert.FromBase64String(value));
        using var gzip = new GZipStream(input, CompressionMode.Decompress);
        return JsonSerializer.Deserialize(
            gzip,
            CommitGraphJsonSerializerContext.Default.FileBlameResponse)!;
    }

    private static FileBlameResponse Response(string content, List<FileBlameHunk> hunks) =>
        new() { Content = content, Hunks = hunks, LineCount = 1, Path = "large.txt", StartCommitHash = "abc" };

    private static string HunkIdentity(FileBlameHunk hunk) =>
        $"{hunk.StartLine}:{hunk.LineCount}:{hunk.Hash}";
}
