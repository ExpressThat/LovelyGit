using System.IO.Compression;
using System.Text;
using ExpressThat.LovelyGit.Services.Git.CommitGraph;
using ExpressThat.LovelyGit.Services.Git.CommitGraph.Models;

namespace LovelyGit.Tests.Git.CommitGraph;

public sealed class CommitPatchPayloadCompactorTests
{
    [Fact]
    public void Compact_RoundTripsLargePatchAndSeriesPayloads()
    {
        var patch = string.Concat(Enumerable.Repeat("+representative patch line\n", 30_000));

        var commit = CommitPatchPayloadCompactor.Compact(new CommitPatchResponse { Patch = patch });
        var series = CommitPatchPayloadCompactor.Compact(new CommitPatchSeriesResponse { Patch = patch });

        Assert.Empty(commit.Patch);
        Assert.Empty(series.Patch);
        Assert.Equal(patch, Expand(commit.CompactPatchGzipBase64));
        Assert.Equal(patch, Expand(series.CompactPatchGzipBase64));
        Assert.True(commit.CompactPatchGzipBase64.Length < 20_000);
    }

    [Fact]
    public void Compact_RejectsAnUnsafeIncompressibleEnvelope()
    {
        var bytes = new byte[700_000];
        new Random(42).NextBytes(bytes);

        var response = CommitPatchPayloadCompactor.Compact(
            new CommitPatchResponse { Patch = Convert.ToBase64String(bytes) });

        Assert.True(response.IsTruncated);
        Assert.Empty(response.Patch);
        Assert.Empty(response.CompactPatchGzipBase64);
    }

    [Fact]
    public void Compact_RemovesUnusablePatchFromTruncatedResponse()
    {
        var response = CommitPatchPayloadCompactor.Compact(
            new CommitPatchResponse { IsTruncated = true, Patch = "partial" });

        Assert.True(response.IsTruncated);
        Assert.Empty(response.Patch);
    }

    private static string Expand(string value)
    {
        using var input = new MemoryStream(Convert.FromBase64String(value));
        using var gzip = new GZipStream(input, CompressionMode.Decompress);
        using var reader = new StreamReader(gzip, Encoding.UTF8);
        return reader.ReadToEnd();
    }
}
