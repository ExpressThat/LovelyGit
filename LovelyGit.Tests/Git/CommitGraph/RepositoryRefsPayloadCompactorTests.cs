using System.IO.Compression;
using System.Text.Json;
using ExpressThat.LovelyGit.Services.Git.CommitGraph.Models;
using ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.CommitGraph;

namespace LovelyGit.Tests.Git.CommitGraph;

public sealed class RepositoryRefsPayloadCompactorTests
{
    [Fact]
    public void CompactIfUseful_LeavesSmallResponsesUnchanged()
    {
        var response = Response(RepositoryRefsPayloadCompactor.CompressionThreshold - 1);

        Assert.Same(response, RepositoryRefsPayloadCompactor.CompactIfUseful(response));
    }

    [Fact]
    public void CompactIfUseful_RoundTripsEveryLargeRef()
    {
        var response = Response(10_000);

        var compact = RepositoryRefsPayloadCompactor.CompactIfUseful(response);
        var bytes = Convert.FromBase64String(compact.CompactRefsGzipBase64!);
        using var input = new MemoryStream(bytes);
        using var gzip = new GZipStream(input, CompressionMode.Decompress);
        var refs = JsonSerializer.Deserialize(
            gzip,
            CommitGraphJsonSerializerContext.Default.ListRepositoryRefItem);

        Assert.Empty(compact.Refs);
        Assert.Equal(response.Refs, refs);
        Assert.True(compact.CompactRefsGzipBase64!.Length < 200_000);
    }

    [Fact]
    public void CompactIfUseful_CompactsFewUnusuallyLargeRefs()
    {
        var response = Response(1) with
        {
            Refs =
            [
                new RepositoryRefItem
                {
                    CommitHash = new string('a', 40),
                    Kind = CommitRefKind.Local,
                    Name = new string('x', 256_000),
                },
            ],
        };

        Assert.NotNull(RepositoryRefsPayloadCompactor
            .CompactIfUseful(response).CompactRefsGzipBase64);
    }

    private static RepositoryRefsResponse Response(int count) =>
        new()
        {
            CurrentBranchName = "main",
            Refs = Enumerable.Range(0, count)
                .Select(index => new RepositoryRefItem
                {
                    CommitHash = $"{index:x40}",
                    Kind = CommitRefKind.Local,
                    Name = $"perf/branch-{index:D5}",
                })
                .ToList(),
        };
}
