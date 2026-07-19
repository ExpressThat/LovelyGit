using System.IO.Compression;
using System.Text.Json;
using ExpressThat.LovelyGit.Services.Data.Models;
using ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.KnownRepository;

namespace LovelyGit.Tests.NativeMessaging;

public sealed class KnownRepositoriesPayloadCompactorTests
{
    [Fact]
    public void Compact_SmallCollectionPreservesDirectList()
    {
        var repositories = CreateRepositories(
            KnownRepositoriesPayloadCompactor.CompressionThreshold - 1);

        var response = KnownRepositoriesPayloadCompactor.Compact(repositories);

        Assert.Same(repositories, response.Repositories);
        Assert.Null(response.CompactRepositoriesGzipBase64);
    }

    [Fact]
    public void Compact_LargeCollectionRoundTripsWithoutRetainingDirectList()
    {
        var repositories = CreateRepositories(
            KnownRepositoriesPayloadCompactor.CompressionThreshold);

        var response = KnownRepositoriesPayloadCompactor.Compact(repositories);

        Assert.Empty(response.Repositories);
        Assert.NotNull(response.CompactRepositoriesGzipBase64);
        var rawJson = JsonSerializer.SerializeToUtf8Bytes(
            repositories,
            KnownRepositoriesJsonSerializerContext.Default.ListKnownGitRepository);
        Assert.True(response.CompactRepositoriesGzipBase64.Length < rawJson.Length);
        using var source = new MemoryStream(Convert.FromBase64String(
            response.CompactRepositoriesGzipBase64));
        using var gzip = new GZipStream(source, CompressionMode.Decompress);
        var expanded = JsonSerializer.Deserialize(
            gzip,
            KnownRepositoriesJsonSerializerContext.Default.ListKnownGitRepository);
        Assert.Equal(repositories, expanded);
    }

    private static List<KnownGitRepository> CreateRepositories(int count) =>
        Enumerable.Range(0, count)
            .Select(index => new KnownGitRepository
            {
                Id = Guid.CreateVersion7(),
                Name = $"repository-{index:D5}",
                Path = $@"C:\repositories\group-{index % 10}\repository-{index:D5}",
            })
            .ToList();
}
