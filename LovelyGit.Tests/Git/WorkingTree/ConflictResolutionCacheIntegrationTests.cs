using ExpressThat.LovelyGit.Services.Git.CommitGraph.Models;
using ExpressThat.LovelyGit.Services.Git.WorkingTree;
using ExpressThat.LovelyGit.Services.Git.WorkingTree.Models;
using LovelyGit.Tests.Git.RepositoryOperations;

namespace LovelyGit.Tests.Git.WorkingTree;

public sealed class ConflictResolutionCacheIntegrationTests(ConflictRepositoryFixture fixture)
    : IClassFixture<ConflictRepositoryFixture>
{
    [Fact]
    public async Task ReadAsync_ReusesEachUnchangedWhitespaceVariant()
    {
        using var repository = await CreateConflictAsync();
        var service = CreateService(repository);

        var exact = await ReadAsync(service, repository, ignoreWhitespace: false);
        var exactAgain = await ReadAsync(service, repository, ignoreWhitespace: false);
        var ignored = await ReadAsync(service, repository, ignoreWhitespace: true);
        var ignoredAgain = await ReadAsync(service, repository, ignoreWhitespace: true);

        Assert.Same(exact, exactAgain);
        Assert.NotSame(exact, ignored);
        Assert.Same(ignored, ignoredAgain);
        Assert.Equal(exact.WorktreeFingerprint, ignored.WorktreeFingerprint);
        Assert.Equal(exact.Hunks, ignored.Hunks);
        Assert.Equal(exact.CurrentSource, ignored.CurrentSource);
        Assert.Equal(exact.IncomingSource, ignored.IncomingSource);
        Assert.Equal(exact.Base.Text, ignored.Base.Text);
        Assert.Equal(exact.Ours.Text, ignored.Ours.Text);
        Assert.Equal(exact.Theirs.Text, ignored.Theirs.Text);
        Assert.Equal(exact.Result.Text, ignored.Result.Text);
    }

    [Fact]
    public async Task ReadAsync_DropsCachedVariantsWhenWorktreeChanges()
    {
        using var repository = await CreateConflictAsync();
        var service = CreateService(repository);
        var original = await ReadAsync(service, repository, ignoreWhitespace: false);
        await File.AppendAllTextAsync(Path.Combine(repository.Path, "shared.txt"), "\nexternal edit");

        var changed = await ReadAsync(service, repository, ignoreWhitespace: false);

        Assert.NotSame(original, changed);
        Assert.NotEqual(original.WorktreeFingerprint, changed.WorktreeFingerprint);
        Assert.Contains("external edit", changed.Result.Text);
    }

    [Fact]
    public async Task CachedRead_StillHonorsCancellation()
    {
        using var repository = await CreateConflictAsync();
        var service = CreateService(repository);
        await ReadAsync(service, repository, ignoreWhitespace: false);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => service.ReadAsync(
            repository.Path,
            "shared.txt",
            CommitDiffViewMode.SideBySide,
            ignoreWhitespace: false,
            cancellation.Token));
    }

    private static ConflictResolutionService CreateService(TestRepository repository) =>
        new(new WorkingTreeIndexService(repository.Git));

    private static Task<ConflictResolutionResponse> ReadAsync(
        ConflictResolutionService service,
        TestRepository repository,
        bool ignoreWhitespace) => service.ReadAsync(
            repository.Path,
            "shared.txt",
            CommitDiffViewMode.SideBySide,
            ignoreWhitespace,
            CancellationToken.None);

    private Task<TestRepository> CreateConflictAsync() => Task.FromResult(fixture.CreateCopy());
}
