using ExpressThat.LovelyGit.Services.Git.Cli;
using ExpressThat.LovelyGit.Services.Git.Tags;
using LovelyGit.Tests.Git.Branches;

namespace LovelyGit.Tests.Git.Tags;

public sealed class GitTagCommandServiceFailureTests
{
    [Theory]
    [InlineData("bad tag", "Tag name is not valid")]
    [InlineData("refs/tags/v1", "Tag name is not valid")]
    public async Task CreateTagAsync_InvalidNameDoesNotCreateARef(
        string tagName,
        string expectedMessage)
    {
        using var repository = TemporaryGitRepository.Create();
        var service = new GitTagCommandService(
            new GitOperationService(repository.GitCliService));

        var exception = await Assert.ThrowsAsync<ArgumentException>(() => service.CreateTagAsync(
            repository.Path,
            tagName,
            repository.HeadCommitHash,
            false,
            false,
            null,
            CancellationToken.None));

        Assert.Contains(expectedMessage, exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(await ReadTagsAsync(repository));
    }

    [Fact]
    public async Task CreateTagAsync_AnnotatedTagRequiresMessageWithoutCreatingARef()
    {
        using var repository = TemporaryGitRepository.Create();
        var service = new GitTagCommandService(
            new GitOperationService(repository.GitCliService));

        var exception = await Assert.ThrowsAsync<ArgumentException>(() => service.CreateTagAsync(
            repository.Path,
            "v-missing-message",
            repository.HeadCommitHash,
            true,
            false,
            "  ",
            CancellationToken.None));

        Assert.Contains("message is required", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(await ReadTagsAsync(repository));
    }

    [Fact]
    public async Task CreateTagAsync_MissingCommitDoesNotCreateTagOrMoveHead()
    {
        using var repository = TemporaryGitRepository.Create();
        var service = new GitTagCommandService(
            new GitOperationService(repository.GitCliService));

        var exception = await Assert.ThrowsAsync<ArgumentException>(() => service.CreateTagAsync(
            repository.Path,
            "v-missing-commit",
            new string('0', 40),
            false,
            false,
            null,
            CancellationToken.None));

        Assert.Contains("does not exist", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(await ReadTagsAsync(repository));
        Assert.Equal(repository.HeadCommitHash, await ReadHeadAsync(repository));
    }

    [Fact]
    public async Task DeleteTagAsync_MissingTagPreservesExistingTags()
    {
        using var repository = TemporaryGitRepository.Create();
        var service = new GitTagCommandService(
            new GitOperationService(repository.GitCliService));
        await service.CreateTagAsync(
            repository.Path, "v-keep", repository.HeadCommitHash, false, false, null,
            CancellationToken.None);

        await Assert.ThrowsAsync<GitOperationException>(() => service.DeleteTagAsync(
            repository.Path, "v-missing", CancellationToken.None));

        Assert.Equal(["v-keep"], await ReadTagsAsync(repository));
    }

    [Fact]
    public async Task PushTagAsync_InvalidRemotePreservesLocalTag()
    {
        using var repository = TemporaryGitRepository.Create();
        var service = new GitTagCommandService(
            new GitOperationService(repository.GitCliService));
        await service.CreateTagAsync(
            repository.Path, "v-local", repository.HeadCommitHash, false, false, null,
            CancellationToken.None);

        await Assert.ThrowsAsync<ArgumentException>(() => service.PushTagAsync(
            repository.Path, "bad remote", "v-local", CancellationToken.None));

        Assert.Equal(["v-local"], await ReadTagsAsync(repository));
    }

    private static async Task<string> ReadHeadAsync(TemporaryGitRepository repository)
    {
        var result = await repository.GitCliService.ExecuteBufferedAsync(
            ["rev-parse", "HEAD"], repository.Path, cancellationToken: CancellationToken.None);
        return result.StandardOutput.Trim();
    }

    private static async Task<string[]> ReadTagsAsync(TemporaryGitRepository repository)
    {
        var result = await repository.GitCliService.ExecuteBufferedAsync(
            ["tag", "--list"], repository.Path, cancellationToken: CancellationToken.None);
        return result.StandardOutput.Split(
            ['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }
}
