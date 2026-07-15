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
        var exception = await AssertInvalidDoesNotMutateAsync(path =>
            CreateService().CreateTagAsync(
                path,
                tagName,
                new string('1', 40),
                false,
                false,
                null,
                CancellationToken.None));

        Assert.Contains(expectedMessage, exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreateTagAsync_AnnotatedTagRequiresMessageWithoutCreatingARef()
    {
        var exception = await AssertInvalidDoesNotMutateAsync(path =>
            CreateService().CreateTagAsync(
                path,
                "v-missing-message",
                new string('1', 40),
                true,
                false,
                "  ",
                CancellationToken.None));

        Assert.Contains("message is required", exception.Message, StringComparison.OrdinalIgnoreCase);
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
    public async Task CreateTagAsync_NonCommitObjectDoesNotCreateTagOrMoveHead()
    {
        using var repository = TemporaryGitRepository.Create();
        var blobPath = Path.Combine(repository.Path, "blob.txt");
        await File.WriteAllTextAsync(blobPath, "not a commit");
        var blobHash = (await repository.GitCliService.ExecuteBufferedAsync(
            ["hash-object", "-w", "--", "blob.txt"],
            repository.Path,
            cancellationToken: CancellationToken.None)).StandardOutput.Trim();
        var service = new GitTagCommandService(
            new GitOperationService(repository.GitCliService));

        var exception = await Assert.ThrowsAsync<InvalidDataException>(() => service.CreateTagAsync(
            repository.Path,
            "v-invalid-target",
            blobHash,
            false,
            false,
            null,
            CancellationToken.None));

        Assert.Contains("not a commit", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(await ReadTagsAsync(repository));
        Assert.Equal(repository.HeadCommitHash, await ReadHeadAsync(repository));
    }

    [Fact]
    public async Task CreateTagAsync_CancellationDoesNotCreateTagOrMoveHead()
    {
        using var repository = TemporaryGitRepository.Create();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var service = new GitTagCommandService(
            new GitOperationService(repository.GitCliService));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => service.CreateTagAsync(
            repository.Path,
            "v-cancelled",
            repository.HeadCommitHash,
            false,
            false,
            null,
            cancellation.Token));

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
    public async Task PushTagAsync_InvalidRemoteIsRejectedBeforeRepositoryMutation()
    {
        await AssertInvalidDoesNotMutateAsync(path => CreateService().PushTagAsync(
            path,
            "bad remote",
            "v-local",
            CancellationToken.None));
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

    private static GitTagCommandService CreateService() =>
        new(new GitOperationService(new GitCliService()));

    private static async Task<ArgumentException> AssertInvalidDoesNotMutateAsync(
        Func<string, Task> action)
    {
        var directory = Directory.CreateTempSubdirectory("lovelygit-invalid-tag-command-");
        var sentinel = Path.Combine(directory.FullName, "sentinel.txt");
        await File.WriteAllTextAsync(sentinel, "unchanged");
        try
        {
            var error = await Assert.ThrowsAsync<ArgumentException>(() => action(directory.FullName));
            Assert.Equal("unchanged", await File.ReadAllTextAsync(sentinel));
            Assert.False(Directory.Exists(Path.Combine(directory.FullName, ".git")));
            return error;
        }
        finally
        {
            directory.Delete(recursive: true);
        }
    }
}
