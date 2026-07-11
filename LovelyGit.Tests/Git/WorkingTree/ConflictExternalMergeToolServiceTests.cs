using ExpressThat.LovelyGit.Services.Git.WorkingTree;
using LovelyGit.Tests.Git.RepositoryOperations;

namespace LovelyGit.Tests.Git.WorkingTree;

public sealed class ConflictExternalMergeToolServiceTests
{
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
        await repository.Git.ExecuteBufferedAsync(
            ["config", "merge.tool", "lovelygit-missing"], repository.Path);
        var state = await CaptureStateAsync(repository);

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            CreateService(repository).OpenAsync(repository.Path, "shared.txt", CancellationToken.None));

        Assert.Contains("merge tool", error.Message, StringComparison.OrdinalIgnoreCase);
        await AssertStateUnchangedAsync(repository, state);
    }

    [Fact]
    public async Task OpenAsync_RollsBackFileAndIndexWhenToolFails()
    {
        using var repository = await CreateConflictAsync();
        await ConfigureToolAsync(repository, "printf 'tool failed\\n' >&2; exit 7");
        var state = await CaptureStateAsync(repository);

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            CreateService(repository).OpenAsync(repository.Path, "shared.txt", CancellationToken.None));

        Assert.Contains("tool failed", error.Message, StringComparison.OrdinalIgnoreCase);
        await AssertStateUnchangedAsync(repository, state);
    }

    [Fact]
    public async Task OpenAsync_RollsBackToolThatStagesUnchangedConflictMarkers()
    {
        using var repository = await CreateConflictAsync();
        await ConfigureToolAsync(repository, "true");
        var state = await CaptureStateAsync(repository);

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            CreateService(repository).OpenAsync(repository.Path, "shared.txt", CancellationToken.None));

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

    private static ConflictExternalMergeToolService CreateService(TestRepository repository) =>
        new(repository.Git);

    private static async Task<TestRepository> CreateConflictAsync()
    {
        var repository = TestRepository.Create();
        await repository.CreateBranchCommitAsync("conflict", "shared.txt", "feature");
        await repository.SwitchAsync("main");
        await repository.CommitFileAsync("shared.txt", "main", "main conflict");
        Assert.False((await repository.Service.MergeAsync(
            repository.Path,
            "conflict",
            CancellationToken.None)).IsCompleted);
        return repository;
    }

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

}
