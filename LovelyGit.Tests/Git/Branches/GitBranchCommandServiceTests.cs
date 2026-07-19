using ExpressThat.LovelyGit.Services.Git.Branches;
using ExpressThat.LovelyGit.Services.Git.Cli;

namespace LovelyGit.Tests.Git.Branches;

public sealed class GitBranchCommandServiceTests
{
    [Fact]
    public async Task CreateBranchAsync_CreatesBranchAtCommit()
    {
        using var repository = TemporaryGitRepository.Create();
        var branchService = new GitBranchCommandService(repository.GitCliService);

        await branchService.CreateBranchAsync(
            repository.Path,
            "feature/from-commit",
            repository.HeadCommitHash,
            CancellationToken.None);

        var branchRef = await repository.GitCliService.ExecuteBufferedAsync(
            ["show-ref", "--verify", "refs/heads/feature/from-commit"],
            repository.Path,
            cancellationToken: CancellationToken.None);

        Assert.StartsWith(repository.HeadCommitHash, branchRef.StandardOutput);
    }

    [Fact]
    public async Task CreateBranchAsync_CreatesAtBranchWithoutSwitching()
    {
        using var repository = TemporaryGitRepository.Create();
        var branchService = new GitBranchCommandService(repository.GitCliService);
        var currentBranch = await repository.GitCliService.ExecuteBufferedAsync(
            ["branch", "--show-current"],
            repository.Path,
            cancellationToken: CancellationToken.None);

        await branchService.CreateBranchAsync(
            repository.Path,
            "feature/stay-here",
            currentBranch.StandardOutput.Trim(),
            CancellationToken.None);

        var branchRef = await repository.GitCliService.ExecuteBufferedAsync(
            ["show-ref", "--verify", "refs/heads/feature/stay-here"],
            repository.Path,
            cancellationToken: CancellationToken.None);
        var branchAfter = await repository.GitCliService.ExecuteBufferedAsync(
            ["branch", "--show-current"],
            repository.Path,
            cancellationToken: CancellationToken.None);

        Assert.StartsWith(repository.HeadCommitHash, branchRef.StandardOutput);
        Assert.Equal(currentBranch.StandardOutput.Trim(), branchAfter.StandardOutput.Trim());
    }

    [Fact]
    public async Task CreateBranchFromTagAsync_CreatesBranchAtTag()
    {
        using var repository = TemporaryGitRepository.Create();
        var branchService = new GitBranchCommandService(repository.GitCliService);
        await repository.GitCliService.ExecuteBufferedAsync(
            ["tag", "v-test-branch-source", repository.HeadCommitHash],
            repository.Path,
            cancellationToken: CancellationToken.None);

        await branchService.CreateBranchFromTagAsync(
            repository.Path,
            "feature/from-tag",
            "v-test-branch-source",
            CancellationToken.None);

        var branchRef = await repository.GitCliService.ExecuteBufferedAsync(
            ["show-ref", "--verify", "refs/heads/feature/from-tag"],
            repository.Path,
            cancellationToken: CancellationToken.None);

        Assert.StartsWith(repository.HeadCommitHash, branchRef.StandardOutput);
    }

    [Fact]
    public async Task RenameBranchAsync_RenamesLocalBranch()
    {
        using var repository = TemporaryGitRepository.Create();
        var branchService = new GitBranchCommandService(repository.GitCliService);

        await branchService.CreateBranchAsync(
            repository.Path,
            "feature/old-name",
            repository.HeadCommitHash,
            CancellationToken.None);

        await branchService.RenameBranchAsync(
            repository.Path,
            "feature/old-name",
            "feature/new-name",
            CancellationToken.None);

        var renamedRef = await repository.GitCliService.ExecuteBufferedAsync(
            ["show-ref", "--verify", "refs/heads/feature/new-name"],
            repository.Path,
            cancellationToken: CancellationToken.None);
        var oldRef = await repository.GitCliService.ExecuteBufferedAsync(
            ["show-ref", "--verify", "refs/heads/feature/old-name"],
            repository.Path,
            validateExitCode: false,
            cancellationToken: CancellationToken.None);

        Assert.StartsWith(repository.HeadCommitHash, renamedRef.StandardOutput);
        Assert.NotEqual(0, oldRef.ExitCode);
    }

    [Fact]
    public async Task DeleteBranchAsync_DeletesMergedBranch()
    {
        using var repository = TemporaryGitRepository.Create();
        var branchService = new GitBranchCommandService(repository.GitCliService);
        await branchService.CreateBranchAsync(
            repository.Path,
            "feature/delete-me",
            repository.HeadCommitHash,
            CancellationToken.None);

        await branchService.DeleteBranchAsync(
            repository.Path,
            "feature/delete-me",
            force: false,
            CancellationToken.None);

        var branchRef = await repository.GitCliService.ExecuteBufferedAsync(
            ["show-ref", "--verify", "refs/heads/feature/delete-me"],
            repository.Path,
            validateExitCode: false,
            cancellationToken: CancellationToken.None);

        Assert.NotEqual(0, branchRef.ExitCode);
    }

    [Fact]
    public async Task DeleteBranchAsync_SafeFailurePreservesStateAndForceRetryDeletesBranch()
    {
        using var repository = TemporaryGitRepository.Create();
        var branchService = new GitBranchCommandService(repository.GitCliService);
        await repository.CreateUnmergedBranchAsync("feature/force-delete-me");
        var headBefore = await repository.GitCliService.ExecuteBufferedAsync(
            ["rev-parse", "HEAD"],
            repository.Path,
            cancellationToken: CancellationToken.None);
        var statusBefore = await repository.GitCliService.ExecuteBufferedAsync(
            ["status", "--porcelain=v1"],
            repository.Path,
            cancellationToken: CancellationToken.None);

        var error = await Assert.ThrowsAsync<GitOperationException>(() =>
            branchService.DeleteBranchAsync(
                repository.Path,
                "feature/force-delete-me",
                force: false,
                CancellationToken.None));

        var preservedRef = await repository.GitCliService.ExecuteBufferedAsync(
            ["show-ref", "--verify", "refs/heads/feature/force-delete-me"],
            repository.Path,
            cancellationToken: CancellationToken.None);
        Assert.Contains("not fully merged", error.Message, StringComparison.OrdinalIgnoreCase);
        Assert.NotEmpty(preservedRef.StandardOutput);
        Assert.Equal(headBefore.StandardOutput, (await repository.GitCliService.ExecuteBufferedAsync(
            ["rev-parse", "HEAD"],
            repository.Path,
            cancellationToken: CancellationToken.None)).StandardOutput);
        Assert.Equal(statusBefore.StandardOutput, (await repository.GitCliService.ExecuteBufferedAsync(
            ["status", "--porcelain=v1"],
            repository.Path,
            cancellationToken: CancellationToken.None)).StandardOutput);

        await branchService.DeleteBranchAsync(
            repository.Path,
            "feature/force-delete-me",
            force: true,
            CancellationToken.None);

        var branchRef = await repository.GitCliService.ExecuteBufferedAsync(
            ["show-ref", "--verify", "refs/heads/feature/force-delete-me"],
            repository.Path,
            validateExitCode: false,
            cancellationToken: CancellationToken.None);

        Assert.NotEqual(0, branchRef.ExitCode);
    }

    [Fact]
    public async Task PushBranchAsync_PushesBranchToOrigin()
    {
        using var repository = TemporaryGitRepository.Create();
        using var remote = TemporaryBareGitRepository.Create(repository.GitCliService);
        var branchService = new GitBranchCommandService(repository.GitCliService);
        await branchService.CreateBranchAsync(
            repository.Path,
            "feature/push-me",
            repository.HeadCommitHash,
            CancellationToken.None);
        await repository.GitCliService.ExecuteBufferedAsync(
            ["remote", "add", "origin", remote.Path],
            repository.Path,
            cancellationToken: CancellationToken.None);

        await branchService.PushBranchAsync(
            repository.Path,
            "feature/push-me",
            CancellationToken.None);

        var remoteRef = await repository.GitCliService.ExecuteBufferedAsync(
            ["show-ref", "--verify", "refs/heads/feature/push-me"],
            remote.Path,
            cancellationToken: CancellationToken.None);

        Assert.StartsWith(repository.HeadCommitHash, remoteRef.StandardOutput);
    }

}
