using System.IO.Compression;
using System.Text.Json;
using ExpressThat.LovelyGit.Services.Git.BranchComparison;
using ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.BranchComparison;

namespace LovelyGit.Tests.Git.BranchComparison;

public sealed class BranchComparisonPayloadCompactorTests
{
    [Fact]
    public void CompactIfUseful_LeavesSmallResponsesUnchanged()
    {
        var response = Response(BranchComparisonPayloadCompactor.CompressionThreshold - 1);

        Assert.Same(response, BranchComparisonPayloadCompactor.CompactIfUseful(response));
    }

    [Fact]
    public void CompactIfUseful_RoundTripsEveryLargeFile()
    {
        var response = Response(2_001);

        var compact = BranchComparisonPayloadCompactor.CompactIfUseful(response);
        var bytes = Convert.FromBase64String(compact.CompactFilesGzipBase64!);
        using var input = new MemoryStream(bytes);
        using var gzip = new GZipStream(input, CompressionMode.Decompress);
        var files = JsonSerializer.Deserialize(
            gzip,
            BranchComparisonJsonSerializerContext.Default.ListBranchComparisonFile);

        Assert.Empty(compact.Files);
        Assert.Equal(response.Files, files);
        Assert.True(compact.CompactFilesGzipBase64!.Length < 40_000);
    }

    [Fact]
    public void CompactIfUseful_CompactsFewUnusuallyLongPaths()
    {
        var response = Response(1) with
        {
            Files =
            [
                new BranchComparisonFile
                {
                    Path = new string('x', 128_000),
                    Status = "Modified",
                },
            ],
        };

        Assert.NotNull(BranchComparisonPayloadCompactor
            .CompactIfUseful(response).CompactFilesGzipBase64);
    }

    private static BranchComparisonResponse Response(int count) =>
        new()
        {
            ChangedFileCount = count,
            Files = Enumerable.Range(0, count)
                .Select(index => new BranchComparisonFile
                {
                    Path = $"src/generated/file-{index:D5}.txt",
                    Status = index % 2 == 0 ? "Modified" : "Added",
                })
                .ToList(),
        };
}
