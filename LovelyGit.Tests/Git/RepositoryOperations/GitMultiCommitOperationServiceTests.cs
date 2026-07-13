using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser.Operations;

namespace LovelyGit.Tests.Git.RepositoryOperations;

public sealed class GitMultiCommitOperationServiceTests
{
    [Fact]
    public async Task CherryPickAsync_AppliesCommitsInProvidedOldestFirstOrder()
    {
        using var repository = TestRepository.Create();
        await repository.CreateBranchCommitAsync("feature", "one.txt", "one");
        var first = await repository.GetHeadHashAsync();
        await repository.CommitFileAsync("two.txt", "two", "two");
        var second = await repository.GetHeadHashAsync();
        await repository.SwitchAsync("main");

        var outcome = await repository.Service.CherryPickAsync(
            repository.Path, [first, second], CancellationToken.None);
        var log = await repository.Git.ExecuteBufferedAsync(
            ["log", "-2", "--format=%s"], repository.Path,
            cancellationToken: CancellationToken.None);

        Assert.True(outcome.IsCompleted);
        Assert.Equal(
            ["two", "feature change"],
            log.StandardOutput.Split('\n', StringSplitOptions.RemoveEmptyEntries));
        Assert.True(File.Exists(Path.Combine(repository.Path, "one.txt")));
        Assert.True(File.Exists(Path.Combine(repository.Path, "two.txt")));
    }

    [Fact]
    public async Task RevertAsync_AppliesCommitsInProvidedNewestFirstOrder()
    {
        using var repository = TestRepository.Create();
        await repository.CommitFileAsync("one.txt", "one", "one");
        var first = await repository.GetHeadHashAsync();
        await repository.CommitFileAsync("two.txt", "two", "two");
        var second = await repository.GetHeadHashAsync();

        var outcome = await repository.Service.RevertAsync(
            repository.Path, [second, first], CancellationToken.None);
        var log = await repository.Git.ExecuteBufferedAsync(
            ["log", "-2", "--format=%s"], repository.Path,
            cancellationToken: CancellationToken.None);

        Assert.True(outcome.IsCompleted);
        Assert.Equal(
            ["Revert \"one\"", "Revert \"two\""],
            log.StandardOutput.Split('\n', StringSplitOptions.RemoveEmptyEntries));
        Assert.False(File.Exists(Path.Combine(repository.Path, "one.txt")));
        Assert.False(File.Exists(Path.Combine(repository.Path, "two.txt")));
    }

    [Fact]
    public async Task CherryPickSequenceConflict_AbortRemovesEarlierSequenceCommits()
    {
        using var repository = TestRepository.Create();
        await repository.CreateBranchCommitAsync("feature", "first.txt", "first");
        var first = await repository.GetHeadHashAsync();
        await repository.CommitFileAsync("shared.txt", "feature", "conflict");
        var second = await repository.GetHeadHashAsync();
        await repository.SwitchAsync("main");
        await repository.CommitFileAsync("shared.txt", "main", "main conflict");
        var before = await repository.GetHeadHashAsync();

        var outcome = await repository.Service.CherryPickAsync(
            repository.Path, [first, second], CancellationToken.None);
        Assert.False(outcome.IsCompleted);
        Assert.Equal(GitRepositoryOperationKind.CherryPick, outcome.Operation);
        Assert.True(File.Exists(Path.Combine(repository.Path, "first.txt")));

        await repository.Service.AbortAsync(
            repository.Path, GitRepositoryOperationKind.CherryPick, CancellationToken.None);

        Assert.Equal(before, await repository.GetHeadHashAsync());
        Assert.False(File.Exists(Path.Combine(repository.Path, "first.txt")));
        Assert.Equal("main", await File.ReadAllTextAsync(Path.Combine(repository.Path, "shared.txt")));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    public async Task CommitListValidation_RejectsUnsafeCountsWithoutMutation(int count)
    {
        using var repository = TestRepository.Create();
        var before = await repository.GetHeadHashAsync();
        var hashes = Enumerable.Repeat(before, count).ToList();

        await Assert.ThrowsAsync<ArgumentException>(() => repository.Service.CherryPickAsync(
            repository.Path, hashes, CancellationToken.None));

        Assert.Equal(before, await repository.GetHeadHashAsync());
    }

    [Fact]
    public async Task CommitListValidation_RejectsDuplicatesWithoutMutation()
    {
        using var repository = TestRepository.Create();
        var before = await repository.GetHeadHashAsync();

        await Assert.ThrowsAsync<ArgumentException>(() => repository.Service.RevertAsync(
            repository.Path, [before, before], CancellationToken.None));

        Assert.Equal(before, await repository.GetHeadHashAsync());
    }
}
