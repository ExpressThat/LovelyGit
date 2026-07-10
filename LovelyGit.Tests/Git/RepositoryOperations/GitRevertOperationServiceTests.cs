using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser.Operations;

namespace LovelyGit.Tests.Git.RepositoryOperations;

public sealed class GitRevertOperationServiceTests
{
    [Fact]
    public async Task RevertAsync_CreatesCommitThatReversesSelectedCommit()
    {
        using var repository = TestRepository.Create();
        await repository.CommitFileAsync("shared.txt", "changed", "change shared file");
        var selectedCommit = await repository.GetHeadHashAsync();

        var outcome = await repository.Service.RevertAsync(
            repository.Path,
            selectedCommit,
            CancellationToken.None);
        var subject = await repository.Git.ExecuteBufferedAsync(
            ["show", "-s", "--format=%s", "HEAD"],
            repository.Path,
            cancellationToken: CancellationToken.None);

        Assert.True(outcome.IsCompleted);
        Assert.Null(outcome.Operation);
        Assert.Equal("base", await File.ReadAllTextAsync(Path.Combine(repository.Path, "shared.txt")));
        Assert.StartsWith("Revert", subject.StandardOutput.Trim());
    }

    [Fact]
    public async Task RevertConflict_IsDetectedAndCanBeAborted()
    {
        using var repository = TestRepository.Create();
        await repository.CommitFileAsync("shared.txt", "selected", "selected change");
        var selectedCommit = await repository.GetHeadHashAsync();
        await repository.CommitFileAsync("shared.txt", "newer", "newer change");

        var outcome = await repository.Service.RevertAsync(
            repository.Path,
            selectedCommit,
            CancellationToken.None);

        Assert.False(outcome.IsCompleted);
        Assert.Equal(GitRepositoryOperationKind.Revert, outcome.Operation);
        Assert.Equal(
            GitRepositoryOperationKind.Revert,
            await repository.Service.GetOperationAsync(repository.Path, CancellationToken.None));

        await repository.Service.AbortAsync(
            repository.Path,
            GitRepositoryOperationKind.Revert,
            CancellationToken.None);

        Assert.Null(await repository.Service.GetOperationAsync(repository.Path, CancellationToken.None));
        Assert.Equal("newer", await File.ReadAllTextAsync(Path.Combine(repository.Path, "shared.txt")));
    }

    [Fact]
    public async Task RevertConflict_CanBeResolvedAndContinued()
    {
        using var repository = TestRepository.Create();
        await repository.CommitFileAsync("shared.txt", "selected", "selected change");
        var selectedCommit = await repository.GetHeadHashAsync();
        await repository.CommitFileAsync("shared.txt", "newer", "newer change");
        var paused = await repository.Service.RevertAsync(
            repository.Path,
            selectedCommit,
            CancellationToken.None);
        Assert.False(paused.IsCompleted);

        await File.WriteAllTextAsync(Path.Combine(repository.Path, "shared.txt"), "resolved");
        await repository.RunGitAsync("add", "--", "shared.txt");
        var completed = await repository.Service.ContinueAsync(
            repository.Path,
            GitRepositoryOperationKind.Revert,
            CancellationToken.None);

        Assert.True(completed.IsCompleted);
        Assert.Null(await repository.Service.GetOperationAsync(repository.Path, CancellationToken.None));
        Assert.Equal("resolved", await File.ReadAllTextAsync(Path.Combine(repository.Path, "shared.txt")));
    }
}
