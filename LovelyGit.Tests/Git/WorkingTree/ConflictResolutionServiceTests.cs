using ExpressThat.LovelyGit.Services.Git.CommitGraph.Models;
using ExpressThat.LovelyGit.Services.Git.WorkingTree;
using ExpressThat.LovelyGit.Services.Git.WorkingTree.Models;
using LovelyGit.Tests.Git.RepositoryOperations;

namespace LovelyGit.Tests.Git.WorkingTree;

public sealed class ConflictResolutionServiceTests(ConflictRepositoryFixture fixture)
    : IClassFixture<ConflictRepositoryFixture>
{
    [Fact]
    public async Task ReadAsync_ReturnsThreeStagesBaseComparisonsAndHunks()
    {
        using var repository = await CreateConflictAsync();
        var response = await CreateService(repository).ReadAsync(
            repository.Path,
            "shared.txt",
            CommitDiffViewMode.SideBySide,
            ignoreWhitespace: false,
            CancellationToken.None);

        Assert.Equal("base", response.Base.Text);
        Assert.Equal("main", response.Ours.Text);
        Assert.Equal("feature", response.Theirs.Text);
        Assert.Contains("<<<<<<<", response.Result.Text);
        Assert.NotNull(response.CurrentComparison);
        Assert.NotNull(response.IncomingComparison);
        Assert.True(response.CurrentComparison.HasDifferences);
        Assert.True(response.IncomingComparison.HasDifferences);
        Assert.Single(response.Hunks);
        Assert.Equal("Current", response.CurrentSource.Label);
        Assert.Equal("Incoming", response.IncomingSource.Label);
        Assert.NotNull(response.CurrentSource.ObjectId);
        Assert.NotNull(response.IncomingSource.ObjectId);
        Assert.NotEmpty(response.WorktreeFingerprint);
    }

    [Fact]
    public async Task ResolveAsync_WritesManualResultAndRemovesUnmergedStages()
    {
        using var repository = await CreateConflictAsync();
        var service = CreateService(repository);
        var opened = await ReadAsync(service, repository);

        await service.ResolveAsync(
            repository.Path,
            "shared.txt",
            opened.WorktreeFingerprint,
            "main\nfeature\n",
            source: null,
            deleteResult: false,
            CancellationToken.None);

        Assert.Equal("main\nfeature\n", await File.ReadAllTextAsync(Path.Combine(repository.Path, "shared.txt")));
        Assert.Empty(await ReadUnmergedAsync(repository));
    }

    [Theory]
    [InlineData(ConflictResolutionSource.Ours, "main")]
    [InlineData(ConflictResolutionSource.Theirs, "feature")]
    [InlineData(ConflictResolutionSource.Base, "base")]
    public async Task ResolveAsync_CanUseAWholeConflictStage(ConflictResolutionSource source, string expected)
    {
        using var repository = await CreateConflictAsync();
        var service = CreateService(repository);
        var opened = await ReadAsync(service, repository);

        await service.ResolveAsync(
            repository.Path,
            "shared.txt",
            opened.WorktreeFingerprint,
            resultText: null,
            source,
            deleteResult: false,
            CancellationToken.None);

        Assert.Equal(expected, await File.ReadAllTextAsync(Path.Combine(repository.Path, "shared.txt")));
        Assert.Empty(await ReadUnmergedAsync(repository));
    }

    [Fact]
    public async Task ResolveAsync_CanResolveByDeletingFile()
    {
        using var repository = await CreateConflictAsync();
        var service = CreateService(repository);
        var opened = await ReadAsync(service, repository);

        await service.ResolveAsync(
            repository.Path,
            "shared.txt",
            opened.WorktreeFingerprint,
            resultText: null,
            source: null,
            deleteResult: true,
            CancellationToken.None);

        Assert.False(File.Exists(Path.Combine(repository.Path, "shared.txt")));
        Assert.Empty(await ReadUnmergedAsync(repository));
    }

    [Fact]
    public async Task ResolveAsync_RejectsStaleResultWithoutMutation()
    {
        using var repository = await CreateConflictAsync();
        var service = CreateService(repository);
        var opened = await ReadAsync(service, repository);
        var path = Path.Combine(repository.Path, "shared.txt");
        await File.WriteAllTextAsync(path, "edited elsewhere");

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => service.ResolveAsync(
            repository.Path,
            "shared.txt",
            opened.WorktreeFingerprint,
            "resolved",
            source: null,
            deleteResult: false,
            CancellationToken.None));

        Assert.Contains("changed", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("edited elsewhere", await File.ReadAllTextAsync(path));
        Assert.NotEmpty(await ReadUnmergedAsync(repository));
    }

    [Fact]
    public async Task ResolveAsync_RejectsMarkersAndUnsafePathsWithoutMutation()
    {
        using var repository = await CreateConflictAsync();
        var service = CreateService(repository);
        var opened = await ReadAsync(service, repository);
        var original = opened.Result.Text;

        var markerError = await Assert.ThrowsAsync<InvalidOperationException>(() => service.ResolveAsync(
            repository.Path,
            "shared.txt",
            opened.WorktreeFingerprint,
            "<<<<<<< ours\n=======\n>>>>>>> theirs",
            source: null,
            deleteResult: false,
            CancellationToken.None));
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.ReadAsync(
            repository.Path,
            "../outside.txt",
            CommitDiffViewMode.Combined,
            ignoreWhitespace: false,
            CancellationToken.None));

        Assert.Contains("marker", markerError.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(original, await File.ReadAllTextAsync(Path.Combine(repository.Path, "shared.txt")));
        Assert.NotEmpty(await ReadUnmergedAsync(repository));
    }

    [Fact]
    public async Task ResolveAsync_RestoresOriginalWhenGitCannotStage()
    {
        using var repository = await CreateConflictAsync();
        var service = CreateService(repository);
        var opened = await ReadAsync(service, repository);
        var path = Path.Combine(repository.Path, "shared.txt");
        var original = await File.ReadAllTextAsync(path);
        var lockPath = Path.Combine(repository.Path, ".git", "index.lock");
        await File.WriteAllTextAsync(lockPath, "locked");

        try
        {
            await Assert.ThrowsAnyAsync<Exception>(() => service.ResolveAsync(
                repository.Path,
                "shared.txt",
                opened.WorktreeFingerprint,
                "should roll back",
                source: null,
                deleteResult: false,
                CancellationToken.None));
        }
        finally
        {
            File.Delete(lockPath);
        }

        Assert.Equal(original, await File.ReadAllTextAsync(path));
        Assert.NotEmpty(await ReadUnmergedAsync(repository));
        Assert.Empty(Directory.EnumerateFiles(repository.Path, "*.lovelygit-*"));
    }

    [Fact]
    public async Task ResolveAsync_CancellationLeavesWorktreeAndIndexUnchanged()
    {
        using var repository = await CreateConflictAsync();
        var service = CreateService(repository);
        var opened = await ReadAsync(service, repository);
        var path = Path.Combine(repository.Path, "shared.txt");
        var original = await File.ReadAllTextAsync(path);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => service.ResolveAsync(
            repository.Path,
            "shared.txt",
            opened.WorktreeFingerprint,
            "cancelled result",
            source: null,
            deleteResult: false,
            cancellation.Token));

        Assert.Equal(original, await File.ReadAllTextAsync(path));
        Assert.NotEmpty(await ReadUnmergedAsync(repository));
    }

    private static ConflictResolutionService CreateService(TestRepository repository) =>
        new(new WorkingTreeIndexService(repository.Git));

    private static Task<ConflictResolutionResponse> ReadAsync(
        ConflictResolutionService service,
        TestRepository repository) => service.ReadAsync(
            repository.Path,
            "shared.txt",
            CommitDiffViewMode.Combined,
            ignoreWhitespace: false,
            CancellationToken.None);

    private Task<TestRepository> CreateConflictAsync() => Task.FromResult(fixture.CreateCopy());

    private static async Task<string> ReadUnmergedAsync(TestRepository repository)
    {
        var result = await repository.Git.ExecuteBufferedAsync(
            ["ls-files", "--unmerged"],
            repository.Path,
            cancellationToken: CancellationToken.None);
        return result.StandardOutput;
    }
}
