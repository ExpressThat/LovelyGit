using System.Collections;
using System.Reflection;
using BLite.Core.Transactions;
using ExpressThat.LovelyGit.Services.Data;
using ExpressThat.LovelyGit.Services.Data.Models.Git.CommitGraph;
using ExpressThat.LovelyGit.Services.Data.Repositorys;
using LovelyGit.Tests.Git.WorkingTree;

namespace LovelyGit.Tests.Data;

public sealed class BLiteTransactionRetentionTests
{
    [Fact]
    public async Task GraphCacheReleasesSuccessfulAndNoOpTransactions()
    {
        using var directory = TemporaryDirectory.Create("lovelygit-blite-graph-cache-");
        var databasePath = Path.Combine(directory.Path, "cache.blite");
        GitRepoCacheDbContext.RegisterBsonKeys(databasePath);
        using var context = new GitRepoCacheDbContext(databasePath);
        var repository = new CommitGraphRepository(context);
        var repositoryId = Guid.NewGuid();

        await repository.AddSeenAsync(repositoryId, "seen", CancellationToken.None);
        await repository.DeleteFrontierAsync(repositoryId, "missing", CancellationToken.None);

        Assert.Equal(0, GetRegisteredTransactionCount(context));
    }

    [Fact]
    public async Task TrackedCommittedTransactionIsReleasedFromBLiteRegistry()
    {
        using var directory = TemporaryDirectory.Create("lovelygit-blite-retention-");
        var databasePath = Path.Combine(directory.Path, "cache.blite");
        GitRepoCacheDbContext.RegisterBsonKeys(databasePath);
        using var context = new GitRepoCacheDbContext(databasePath);
        using var transaction = context.BeginTransaction();
        using var retention = BLiteTransactionRetention.Track(transaction);

        await context.CommitGraphSeen.InsertAsync(
            CreateEntry("committed"),
            transaction,
            CancellationToken.None);
        await context.SaveChangesAsync(transaction, CancellationToken.None);

        Assert.Equal(1, GetRegisteredTransactionCount(context));
        retention.Dispose();
        Assert.Equal(0, GetRegisteredTransactionCount(context));
    }

    [Fact]
    public void TrackedAbandonedTransactionRollsBackAndIsReleased()
    {
        using var directory = TemporaryDirectory.Create("lovelygit-blite-abandoned-");
        var databasePath = Path.Combine(directory.Path, "cache.blite");
        GitRepoCacheDbContext.RegisterBsonKeys(databasePath);
        using var context = new GitRepoCacheDbContext(databasePath);
        using var transaction = context.BeginTransaction();
        using var retention = BLiteTransactionRetention.Track(transaction);

        Assert.Equal(1, GetRegisteredTransactionCount(context));
        retention.Dispose();

        Assert.Equal(TransactionState.Aborted, transaction.State);
        Assert.Equal(0, GetRegisteredTransactionCount(context));
    }

    private static CommitGraphSeenEntry CreateEntry(string hash) => new()
    {
        Id = $"repository:{hash}",
        RepositoryId = Guid.NewGuid(),
        Hash = hash,
    };

    private static int GetRegisteredTransactionCount(GitRepoCacheDbContext context)
    {
        var storage = typeof(BLite.Core.DocumentDbContext)
            .GetField("_storage", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(context)!;
        var transactions = (ICollection)storage.GetType()
            .GetField("_activeTransactions", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(storage)!;
        return transactions.Count;
    }
}
