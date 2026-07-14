using System.Diagnostics;
using ExpressThat.LovelyGit.Services.Data;
using ExpressThat.LovelyGit.Services.Data.Repositorys;
using ExpressThat.LovelyGit.Services.Git.CommitGraph;
using ExpressThat.LovelyGit.Services.Git.CommitGraph.Models;
using ExpressThat.LovelyGit.Services.Git.CommitGraph.Queries;
using LovelyGit.Tests.Git.WorkingTree;
using Xunit.Abstractions;

namespace LovelyGit.Tests.Git.CommitGraph;

[Collection(PerformanceTestCollection.Name)]
public sealed class CommitGraphPageServiceCacheTests(ITestOutputHelper output)
{
    [Fact]
    public async Task RepeatGraphReset_PreservesImmutableDetailsAndClearsTraversalState()
    {
        using var directory = TemporaryDirectory.Create("lovelygit-graph-reset-");
        var databasePath = Path.Combine(directory.Path, "cache.blite");
        GitRepoCacheDbContext.RegisterBsonKeys(databasePath);
        using var context = new GitRepoCacheDbContext(databasePath);
        var cache = new CommitGraphRepository(context);
        using var diffService = new CommitFileDiffService(cache);
        using var preloadService = new CommitDetailsPreloadService(
            cache,
            new CommitDetailsService(cache));
        using var service = new CommitGraphPageService(
            null!,
            cache,
            preloadService,
            diffService,
            new CommitGraphBackgroundWorkerOptions(false, false, false));
        var repositoryId = Guid.NewGuid();
        const string hash = "0123456789012345678901234567890123456789";
        await service.ResetRepositoryGraphAsync(repositoryId, CancellationToken.None);
        await cache.SaveRepositoryStateAsync(
            repositoryId, 10, 2, "lanes", CancellationToken.None);
        await cache.SaveCommitDetailsAsync(
            repositoryId, hash, CreateDetails(1_000), CancellationToken.None);
        await cache.SaveCommitFileDiffAsync(
            repositoryId,
            hash,
            "src/file-00000.txt",
            new CommitFileDiffResponse
            {
                CommitHash = hash,
                Path = "src/file-00000.txt",
                Status = "Modified",
                ViewMode = CommitDiffViewMode.SideBySide,
                HasDifferences = true,
                CompactLineSchema = "tuple-v2:gzip-base64:utf-8",
                CompactLinesGzipBase64 = "cached-diff",
                CompactLineCount = 1,
            },
            ignoreWhitespace: false,
            CancellationToken.None);

        var startedAt = Stopwatch.GetTimestamp();
        await service.ResetRepositoryGraphAsync(repositoryId, CancellationToken.None);
        var elapsed = Stopwatch.GetElapsedTime(startedAt);

        output.WriteLine($"Repeat graph reset: {elapsed.TotalMilliseconds:N1} ms");
        Assert.Null(await cache.GetRepositoryStateAsync(repositoryId, CancellationToken.None));
        Assert.NotNull(await cache.GetCommitDetailsAsync(
            repositoryId, hash, CancellationToken.None));
        Assert.NotNull(await cache.GetCommitFileDiffAsync(
            repositoryId,
            hash,
            "src/file-00000.txt",
            CommitDiffViewMode.SideBySide,
            ignoreWhitespace: false,
            CancellationToken.None));
        Assert.True(elapsed < TimeSpan.FromMilliseconds(500), $"Graph reset took {elapsed}.");
    }

    private static CommitDetailsResponse CreateDetails(int fileCount) =>
        new()
        {
            Hash = "hash",
            Subject = "Cached details",
            ChangedFiles = Enumerable.Range(0, fileCount)
                .Select(index => new CommitChangedFile
                {
                    Path = $"src/file-{index:D5}.txt",
                    Status = "Modified",
                })
                .ToList(),
        };
}
