using System.Diagnostics;
using ExpressThat.LovelyGit.Services.Git.WorkingTree;
using LovelyGit.Tests.Git.RepositoryOperations;

namespace LovelyGit.Tests.Git.WorkingTree;

[Collection(GitShellIntegrationCollection.Name)]
public sealed class ConflictExternalMergeToolServiceTests
    : IClassFixture<ConflictRepositoryFixture>
{
    private readonly ConflictRepositoryFixture _fixture;

    public ConflictExternalMergeToolServiceTests(ConflictRepositoryFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task OpenAsync_UsesConfiguredToolAndLeavesResolvedFileStaged()
    {
        using var repository = await CreateConflictAsync();
        await ConfigureToolAsync(repository, "cp \"$LOCAL\" \"$MERGED\"");

        await CreateService(repository).OpenAsync(repository.Path, "shared.txt", CancellationToken.None);

        Assert.Equal("main", await File.ReadAllTextAsync(Path.Combine(repository.Path, "shared.txt")));
        Assert.Equal(string.Empty, await ReadUnmergedAsync(repository));
    }

    [Fact]
    public async Task OpenAsync_MissingConfiguredToolLeavesConflictUntouched()
    {
        using var repository = await CreateConflictAsync();
        var state = await CaptureStateAsync(repository);
        var runner = StubRunner((_, _, _) => Task.FromResult(
            new ConflictMergeToolResult(1, string.Empty, "merge tool is not configured")));

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new ConflictExternalMergeToolService(runner)
                .OpenAsync(repository.Path, "shared.txt", CancellationToken.None));

        Assert.Contains("merge tool", error.Message, StringComparison.OrdinalIgnoreCase);
        await AssertStateUnchangedAsync(repository, state);
    }

    [Fact]
    public async Task OpenAsync_RollsBackFileAndIndexWhenToolFails()
    {
        using var repository = await CreateConflictAsync();
        var state = await CaptureStateAsync(repository);
        var runner = StubRunner(async (root, path, _) =>
        {
            await File.WriteAllTextAsync(Path.Combine(root, path), "tool mutation");
            return new ConflictMergeToolResult(7, string.Empty, "tool failed");
        });

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new ConflictExternalMergeToolService(runner)
                .OpenAsync(repository.Path, "shared.txt", CancellationToken.None));

        Assert.Contains("tool failed", error.Message, StringComparison.OrdinalIgnoreCase);
        await AssertStateUnchangedAsync(repository, state);
    }

    [Fact]
    public async Task OpenAsync_RollsBackToolThatStagesUnchangedConflictMarkers()
    {
        using var repository = await CreateConflictAsync();
        var state = await CaptureStateAsync(repository);
        var runner = StubRunner(async (root, path, cancellationToken) =>
        {
            await repository.Git.ExecuteBufferedAsync(
                ["add", "--", path],
                root,
                cancellationToken: cancellationToken);
            return new ConflictMergeToolResult(0, string.Empty, string.Empty);
        });

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new ConflictExternalMergeToolService(runner)
                .OpenAsync(repository.Path, "shared.txt", CancellationToken.None));

        Assert.Contains("conflict markers", error.Message, StringComparison.OrdinalIgnoreCase);
        await AssertStateUnchangedAsync(repository, state);
    }

    [Fact]
    public async Task OpenAsync_CancellationAndUnsafePathDoNotMutateConflict()
    {
        using var repository = await CreateConflictAsync();
        var state = await CaptureStateAsync(repository);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            CreateService(repository).OpenAsync(repository.Path, "shared.txt", cancellation.Token));
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            CreateService(repository).OpenAsync(repository.Path, "../outside.txt", CancellationToken.None));

        await AssertStateUnchangedAsync(repository, state);
    }

    [Fact]
    public async Task RepositoryTemplate_CopiesAreIsolated()
    {
        using var first = await CreateConflictAsync();
        using var second = await CreateConflictAsync();

        await File.WriteAllTextAsync(Path.Combine(first.Path, "shared.txt"), "changed");

        Assert.Contains("<<<<<<<", await File.ReadAllTextAsync(
            Path.Combine(second.Path, "shared.txt")));
        Assert.NotEmpty(await ReadUnmergedAsync(second));
    }

    [Fact]
    public async Task OpenAsync_PreflightDoesNotScaleWithUnrelatedRefs()
    {
        using var repository = await CreateConflictAsync();
        var head = (await repository.Git.ExecuteBufferedAsync(
            ["rev-parse", "HEAD"], repository.Path)).StandardOutput.Trim();
        SeedUnrelatedRefs(repository.Path, head, 1_500);
        var service = new ConflictExternalMergeToolService(StubRunner((_, _, _) =>
            Task.FromResult(new ConflictMergeToolResult(1, string.Empty, "expected failure"))));
        GC.Collect();
        var allocatedBefore = GC.GetTotalAllocatedBytes(precise: true);
        var startedAt = Stopwatch.GetTimestamp();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.OpenAsync(repository.Path, "shared.txt", CancellationToken.None));

        var elapsed = Stopwatch.GetElapsedTime(startedAt);
        var allocated = GC.GetTotalAllocatedBytes(true) - allocatedBefore;
        Console.WriteLine(
            $"ElapsedMs={elapsed.TotalMilliseconds:F2}; AllocatedBytes={allocated:N0}");
        Assert.True(elapsed < TimeSpan.FromMilliseconds(100), $"Preflight took {elapsed}.");
        Assert.True(allocated < 500_000, $"Preflight allocated {allocated:N0} bytes.");
    }

    private static ConflictExternalMergeToolService CreateService(TestRepository repository) =>
        new(repository.Git);

    private static IConflictMergeToolRunner StubRunner(
        Func<string, string, CancellationToken, Task<ConflictMergeToolResult>> run) =>
        new StubConflictMergeToolRunner(run);

    private Task<TestRepository> CreateConflictAsync() =>
        Task.FromResult(_fixture.CreateCopy());

    private static async Task ConfigureToolAsync(TestRepository repository, string command)
    {
        await repository.Git.ExecuteBufferedAsync(
            ["config", "merge.tool", "lovelygit-test"], repository.Path);
        await repository.Git.ExecuteBufferedAsync(
            ["config", "mergetool.lovelygit-test.cmd", command], repository.Path);
        await repository.Git.ExecuteBufferedAsync(
            ["config", "mergetool.lovelygit-test.trustExitCode", "true"], repository.Path);
    }

    private static async Task<(byte[] File, byte[] Index, string Unmerged)> CaptureStateAsync(
        TestRepository repository) =>
        (
            await File.ReadAllBytesAsync(Path.Combine(repository.Path, "shared.txt")),
            await File.ReadAllBytesAsync(Path.Combine(repository.Path, ".git", "index")),
            await ReadUnmergedAsync(repository)
        );

    private static async Task AssertStateUnchangedAsync(
        TestRepository repository,
        (byte[] File, byte[] Index, string Unmerged) state)
    {
        Assert.Equal(state.File, await File.ReadAllBytesAsync(Path.Combine(repository.Path, "shared.txt")));
        Assert.Equal(state.Index, await File.ReadAllBytesAsync(Path.Combine(repository.Path, ".git", "index")));
        Assert.Equal(state.Unmerged, await ReadUnmergedAsync(repository));
    }

    private static async Task<string> ReadUnmergedAsync(TestRepository repository) =>
        (await repository.Git.ExecuteBufferedAsync(
            ["ls-files", "--unmerged"], repository.Path)).StandardOutput;

    private static void SeedUnrelatedRefs(string repositoryPath, string commit, int count)
    {
        var heads = Directory.CreateDirectory(
            Path.Combine(repositoryPath, ".git", "refs", "heads", "perf"));
        for (var index = 0; index < count; index++)
        {
            File.WriteAllText(Path.Combine(heads.FullName, $"branch-{index:D4}"), commit + "\n");
        }
    }

    private sealed class StubConflictMergeToolRunner(
        Func<string, string, CancellationToken, Task<ConflictMergeToolResult>> run)
        : IConflictMergeToolRunner
    {
        public Task<ConflictMergeToolResult> RunAsync(
            string repositoryPath,
            string path,
            CancellationToken cancellationToken) => run(repositoryPath, path, cancellationToken);
    }

}
