using ExpressThat.LovelyGit.Services.Git.Cli;
using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser.Operations;

namespace LovelyGit.Tests.Git.RepositoryOperations;

public sealed class GitRepositoryOperationServiceTests
{
    [Fact]
    public async Task CherryPickAsync_AppliesCommitToCurrentBranch()
    {
        using var repository = TestRepository.Create();
        await repository.CreateBranchCommitAsync("feature", "feature.txt", "feature");
        var featureCommit = await repository.GetHeadHashAsync();
        await repository.SwitchAsync("main");
        await repository.CommitFileAsync("main.txt", "main", "main change");

        var outcome = await repository.Service.CherryPickAsync(
            repository.Path,
            featureCommit,
            CancellationToken.None);

        Assert.True(outcome.IsCompleted);
        Assert.Null(outcome.Operation);
        Assert.True(File.Exists(Path.Combine(repository.Path, "feature.txt")));
        Assert.NotEqual(featureCommit, await repository.GetHeadHashAsync());
    }

    [Fact]
    public async Task CherryPickConflict_IsDetectedAndCanBeAborted()
    {
        using var repository = TestRepository.Create();
        await repository.CreateBranchCommitAsync("conflict", "shared.txt", "feature");
        var conflictCommit = await repository.GetHeadHashAsync();
        await repository.SwitchAsync("main");
        await repository.CommitFileAsync("shared.txt", "main", "main conflict");

        var outcome = await repository.Service.CherryPickAsync(
            repository.Path,
            conflictCommit,
            CancellationToken.None);

        Assert.False(outcome.IsCompleted);
        Assert.Equal(GitRepositoryOperationKind.CherryPick, outcome.Operation);
        Assert.Equal(
            GitRepositoryOperationKind.CherryPick,
            await repository.Service.GetOperationAsync(repository.Path, CancellationToken.None));

        await repository.Service.AbortAsync(
            repository.Path,
            GitRepositoryOperationKind.CherryPick,
            CancellationToken.None);

        Assert.Null(await repository.Service.GetOperationAsync(repository.Path, CancellationToken.None));
        Assert.Equal("main", await File.ReadAllTextAsync(Path.Combine(repository.Path, "shared.txt")));
    }

    [Fact]
    public async Task CherryPickConflict_CanBeResolvedAndContinued()
    {
        using var repository = TestRepository.Create();
        await repository.CreateBranchCommitAsync("conflict", "shared.txt", "feature");
        var conflictCommit = await repository.GetHeadHashAsync();
        await repository.SwitchAsync("main");
        await repository.CommitFileAsync("shared.txt", "main", "main conflict");
        var paused = await repository.Service.CherryPickAsync(
            repository.Path,
            conflictCommit,
            CancellationToken.None);
        Assert.False(paused.IsCompleted);

        await File.WriteAllTextAsync(Path.Combine(repository.Path, "shared.txt"), "resolved");
        await repository.RunGitAsync("add", "--", "shared.txt");
        var completed = await repository.Service.ContinueAsync(
            repository.Path,
            GitRepositoryOperationKind.CherryPick,
            CancellationToken.None);

        Assert.True(completed.IsCompleted);
        Assert.Null(await repository.Service.GetOperationAsync(repository.Path, CancellationToken.None));
        Assert.Equal("resolved", await File.ReadAllTextAsync(Path.Combine(repository.Path, "shared.txt")));
    }

    [Fact]
    public async Task MergeAsync_MergesDivergedBranch()
    {
        using var repository = TestRepository.Create();
        await repository.CreateBranchCommitAsync("feature", "feature.txt", "feature");
        await repository.SwitchAsync("main");
        await repository.CommitFileAsync("main.txt", "main", "main change");

        var outcome = await repository.Service.MergeAsync(
            repository.Path,
            "feature",
            CancellationToken.None);

        Assert.True(outcome.IsCompleted);
        Assert.Null(outcome.Operation);
        Assert.True(File.Exists(Path.Combine(repository.Path, "feature.txt")));
    }

    [Fact]
    public async Task RebaseAsync_ReplaysCurrentBranchOntoSelectedBranch()
    {
        using var repository = TestRepository.Create();
        await repository.CreateBranchCommitAsync("topic", "topic.txt", "topic");
        await repository.SwitchAsync("main");
        await repository.CommitFileAsync("main.txt", "main", "main change");
        await repository.SwitchAsync("topic");

        var outcome = await repository.Service.RebaseAsync(
            repository.Path,
            "main",
            CancellationToken.None);
        var ancestry = await repository.Git.ExecuteBufferedAsync(
            ["merge-base", "--is-ancestor", "main", "topic"],
            repository.Path,
            validateExitCode: false,
            cancellationToken: CancellationToken.None);

        Assert.True(outcome.IsCompleted);
        Assert.Equal(0, ancestry.ExitCode);
    }

    [Fact]
    public async Task MergeConflict_IsDetectedAndCanBeAborted()
    {
        using var repository = TestRepository.Create();
        await repository.CreateBranchCommitAsync("conflict", "shared.txt", "feature");
        await repository.SwitchAsync("main");
        await repository.CommitFileAsync("shared.txt", "main", "main conflict");

        var outcome = await repository.Service.MergeAsync(
            repository.Path,
            "conflict",
            CancellationToken.None);

        Assert.False(outcome.IsCompleted);
        Assert.Equal(GitRepositoryOperationKind.Merge, outcome.Operation);
        Assert.Equal(
            GitRepositoryOperationKind.Merge,
            await repository.Service.GetOperationAsync(repository.Path, CancellationToken.None));

        await repository.Service.AbortAsync(
            repository.Path,
            GitRepositoryOperationKind.Merge,
            CancellationToken.None);

        Assert.Null(await repository.Service.GetOperationAsync(repository.Path, CancellationToken.None));
        Assert.Equal("main", await File.ReadAllTextAsync(Path.Combine(repository.Path, "shared.txt")));
    }

    [Fact]
    public async Task MergeConflict_CanBeResolvedAndContinued()
    {
        using var repository = TestRepository.Create();
        await repository.CreateBranchCommitAsync("conflict", "shared.txt", "feature");
        await repository.SwitchAsync("main");
        await repository.CommitFileAsync("shared.txt", "main", "main conflict");
        var paused = await repository.Service.MergeAsync(
            repository.Path,
            "conflict",
            CancellationToken.None);
        Assert.False(paused.IsCompleted);

        await File.WriteAllTextAsync(Path.Combine(repository.Path, "shared.txt"), "resolved");
        await repository.RunGitAsync("add", "--", "shared.txt");
        var completed = await repository.Service.ContinueAsync(
            repository.Path,
            GitRepositoryOperationKind.Merge,
            CancellationToken.None);

        Assert.True(completed.IsCompleted);
        Assert.Null(await repository.Service.GetOperationAsync(repository.Path, CancellationToken.None));
        Assert.Equal("resolved", await File.ReadAllTextAsync(Path.Combine(repository.Path, "shared.txt")));
    }

}
