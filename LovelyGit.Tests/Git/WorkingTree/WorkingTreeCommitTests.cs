using ExpressThat.LovelyGit.Services.Git.WorkingTree;
using LovelyGit.Tests.Git.Branches;

namespace LovelyGit.Tests.Git.WorkingTree;

public sealed class WorkingTreeCommitTests
{
    [Fact]
    public async Task CommitStagedChangesAsync_CreatesCommitWithTitleAndBody()
    {
        using var repository = TemporaryGitRepository.Create();
        var service = new WorkingTreeIndexService(repository.GitCliService);
        await File.WriteAllTextAsync(Path.Combine(repository.Path, "feature.txt"), "feature");
        await repository.GitCliService.ExecuteBufferedAsync(["add", "feature.txt"], repository.Path);

        await service.CommitStagedChangesAsync(
            repository.Path,
            "Feature title",
            "Feature body",
            amend: false,
            sign: false,
            CancellationToken.None);

        Assert.Equal("Feature title\nFeature body", await ReadHeadMessageAsync(repository));
        Assert.Equal("2", await CountCommitsAsync(repository));
    }

    [Fact]
    public async Task CommitStagedChangesAsync_AmendsMessageWithoutStagedChanges()
    {
        using var repository = TemporaryGitRepository.Create();
        var service = new WorkingTreeIndexService(repository.GitCliService);
        var originalHead = await ReadHeadHashAsync(repository);

        await service.CommitStagedChangesAsync(
            repository.Path,
            "Rewritten title",
            "Rewritten body",
            amend: true,
            sign: false,
            CancellationToken.None);

        Assert.NotEqual(originalHead, await ReadHeadHashAsync(repository));
        Assert.Equal("Rewritten title\nRewritten body", await ReadHeadMessageAsync(repository));
        Assert.Equal("1", await CountCommitsAsync(repository));
    }

    [Fact]
    public async Task CommitStagedChangesAsync_RejectsBlankTitle()
    {
        using var repository = TemporaryGitRepository.Create();
        var service = new WorkingTreeIndexService(repository.GitCliService);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CommitStagedChangesAsync(
                repository.Path,
                "  ",
                string.Empty,
                amend: false,
                sign: false,
                CancellationToken.None));

        Assert.Equal("Commit title is required.", exception.Message);
    }

    [Fact]
    public async Task HeadCommitMessageService_ReadsHeadThroughNativeParser()
    {
        using var repository = TemporaryGitRepository.Create();
        await repository.GitCliService.ExecuteBufferedAsync(
            ["commit", "--allow-empty", "-m", "Native title", "-m", "Native body"],
            repository.Path);

        var message = await new HeadCommitMessageService()
            .GetAsync(repository.Path, CancellationToken.None);

        Assert.Equal(await ReadHeadHashAsync(repository), message.Hash);
        Assert.Equal(1, message.ParentCount);
        Assert.Equal(repository.HeadCommitHash, message.FirstParentHash);
        Assert.Equal("Native title", message.Title);
        Assert.Equal("Native body", message.Body);
    }

    [Fact]
    public async Task HeadCommitMessageService_ResolvesPackedBranchHead()
    {
        using var repository = TemporaryGitRepository.Create();
        await repository.GitCliService.ExecuteBufferedAsync(
            ["pack-refs", "--all"],
            repository.Path);

        var message = await new HeadCommitMessageService()
            .GetAsync(repository.Path, CancellationToken.None);

        Assert.Equal(repository.HeadCommitHash, message.Hash);
        Assert.Equal(0, message.ParentCount);
        Assert.Null(message.FirstParentHash);
        Assert.Equal("Initial", message.Title);
    }

    [Fact]
    public async Task HeadCommitMessageService_ResolvesDetachedHead()
    {
        using var repository = TemporaryGitRepository.Create();
        await repository.GitCliService.ExecuteBufferedAsync(
            ["checkout", "--detach", repository.HeadCommitHash],
            repository.Path);

        var message = await new HeadCommitMessageService()
            .GetAsync(repository.Path, CancellationToken.None);

        Assert.Equal(repository.HeadCommitHash, message.Hash);
        Assert.Equal("Initial", message.Title);
    }

    private static async Task<string> CountCommitsAsync(TemporaryGitRepository repository) =>
        (await repository.GitCliService.ExecuteBufferedAsync(
            ["rev-list", "--count", "HEAD"],
            repository.Path)).StandardOutput.Trim();

    private static async Task<string> ReadHeadHashAsync(TemporaryGitRepository repository) =>
        (await repository.GitCliService.ExecuteBufferedAsync(
            ["rev-parse", "HEAD"],
            repository.Path)).StandardOutput.Trim();

    private static async Task<string> ReadHeadMessageAsync(TemporaryGitRepository repository) =>
        (await repository.GitCliService.ExecuteBufferedAsync(
            ["log", "-1", "--format=%s%n%b"],
            repository.Path)).StandardOutput.Trim();
}
