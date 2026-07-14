using System.Diagnostics;
using ExpressThat.LovelyGit.Services.Data;
using ExpressThat.LovelyGit.Services.Data.Repositorys;
using ExpressThat.LovelyGit.Services.Git.CommitGraph.Models;
using LovelyGit.Tests.Git.WorkingTree;
using Xunit.Abstractions;

namespace LovelyGit.Tests.Data;

public sealed class CommitDetailsCacheRepositoryTests(ITestOutputHelper output)
{
    [Fact]
    public async Task SaveCommitDetails_BulkPersistsAndReplacesLargeFileLists()
    {
        using var directory = TemporaryDirectory.Create("lovelygit-details-cache-");
        var databasePath = Path.Combine(directory.Path, "cache.blite");
        GitRepoCacheDbContext.RegisterBsonKeys(databasePath);
        using var context = new GitRepoCacheDbContext(databasePath);
        var repository = new CommitGraphRepository(context);
        var repositoryId = Guid.NewGuid();
        const string hash = "0123456789012345678901234567890123456789";
        var initial = CreateDetails(2_000, hasLineStats: false);
        var started = Stopwatch.GetTimestamp();

        await repository.SaveCommitDetailsAsync(
            repositoryId,
            hash,
            initial,
            CancellationToken.None);

        var elapsed = Stopwatch.GetElapsedTime(started);
        output.WriteLine($"Saved 2,000 files in {elapsed.TotalMilliseconds:N0} ms");
        started = Stopwatch.GetTimestamp();
        var loaded = await repository.GetCommitDetailsAsync(
            repositoryId,
            hash,
            CancellationToken.None);
        var loadElapsed = Stopwatch.GetElapsedTime(started);
        output.WriteLine($"Loaded 2,000 files in {loadElapsed.TotalMilliseconds:N0} ms");
        var loadedDetails = Assert.IsType<CommitDetailsResponse>(loaded);
        Assert.Equal(initial.ChangedFiles, loadedDetails.ChangedFiles);
        Assert.False(loadedDetails.HasLineStats);
        Assert.True(elapsed < TimeSpan.FromSeconds(3), $"Bulk save took {elapsed}.");
        Assert.True(loadElapsed < TimeSpan.FromSeconds(1), $"Indexed load took {loadElapsed}.");

        var replacement = CreateDetails(3);
        await repository.SaveCommitDetailsAsync(
            repositoryId,
            hash,
            replacement,
            CancellationToken.None);
        loaded = await repository.GetCommitDetailsAsync(
            repositoryId,
            hash,
            CancellationToken.None);
        Assert.Equal(replacement.ChangedFiles, Assert.IsType<CommitDetailsResponse>(loaded).ChangedFiles);
    }

    [Fact]
    public async Task SaveCommitDetails_PersistsEmptyFileList()
    {
        using var directory = TemporaryDirectory.Create("lovelygit-empty-details-cache-");
        var databasePath = Path.Combine(directory.Path, "cache.blite");
        GitRepoCacheDbContext.RegisterBsonKeys(databasePath);
        using var context = new GitRepoCacheDbContext(databasePath);
        var repository = new CommitGraphRepository(context);
        var repositoryId = Guid.NewGuid();
        const string hash = "abcdefabcdefabcdefabcdefabcdefabcdefabcd";

        await repository.SaveCommitDetailsAsync(
            repositoryId,
            hash,
            CreateDetails(0),
            CancellationToken.None);

        var loaded = await repository.GetCommitDetailsAsync(
            repositoryId,
            hash,
            CancellationToken.None);
        Assert.Empty(Assert.IsType<CommitDetailsResponse>(loaded).ChangedFiles);
    }

    private static CommitDetailsResponse CreateDetails(int fileCount, bool hasLineStats = true)
    {
        var files = new List<CommitChangedFile>(fileCount);
        for (var index = 0; index < fileCount; index++)
        {
            files.Add(new CommitChangedFile
            {
                Path = $"src/generated/file-{index:D5}.txt",
                Status = index % 2 == 0 ? "Added" : "Modified",
                Additions = (uint)(index + 1),
                Deletions = (uint)(index % 7),
                IsBinary = index % 101 == 0,
            });
        }

        return new CommitDetailsResponse
        {
            Hash = "hash",
            Subject = "Large details",
            ChangedFiles = files,
            HasLineStats = hasLineStats,
            Stats = new CommitStats { Additions = 1, Deletions = 2 },
        };
    }
}
