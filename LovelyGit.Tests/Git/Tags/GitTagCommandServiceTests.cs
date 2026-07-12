using ExpressThat.LovelyGit.Services.Git.Cli;
using ExpressThat.LovelyGit.Services.Git.Tags;

namespace LovelyGit.Tests.Git.Tags;

public sealed class GitTagCommandServiceTests
{
    [Fact]
    public async Task CreateTagAsync_CreatesTagAtCommit()
    {
        using var repository = TemporaryTagGitRepository.Create();
        var tagService = new GitTagCommandService(repository.GitOperationService);

        await tagService.CreateTagAsync(
            repository.Path,
            "v-test-create-tag",
            repository.HeadCommitHash,
            isAnnotated: false,
            sign: false,
            message: string.Empty,
            CancellationToken.None);

        var tagRef = await repository.GitCliService.ExecuteBufferedAsync(
            ["show-ref", "--verify", "refs/tags/v-test-create-tag"],
            repository.Path,
            cancellationToken: CancellationToken.None);

        Assert.StartsWith(repository.HeadCommitHash, tagRef.StandardOutput);
    }

    [Fact]
    public async Task CreateTagAsync_CreatesAnnotatedTagAtCommit()
    {
        using var repository = TemporaryTagGitRepository.Create();
        var tagService = new GitTagCommandService(repository.GitOperationService);

        await tagService.CreateTagAsync(
            repository.Path,
            "v-test-annotated-tag",
            repository.HeadCommitHash,
            isAnnotated: true,
            sign: false,
            message: "LovelyGit annotated tag",
            CancellationToken.None);

        var tagType = await repository.GitCliService.ExecuteBufferedAsync(
            ["cat-file", "-t", "v-test-annotated-tag"],
            repository.Path,
            cancellationToken: CancellationToken.None);
        var tagMessage = await repository.GitCliService.ExecuteBufferedAsync(
            ["tag", "-l", "v-test-annotated-tag", "--format=%(contents)"],
            repository.Path,
            cancellationToken: CancellationToken.None);

        Assert.Equal("tag", tagType.StandardOutput.Trim());
        Assert.Contains("LovelyGit annotated tag", tagMessage.StandardOutput);
    }

    [Fact]
    public async Task DeleteTagAsync_DeletesLocalTag()
    {
        using var repository = TemporaryTagGitRepository.Create();
        var tagService = new GitTagCommandService(repository.GitOperationService);
        await tagService.CreateTagAsync(
            repository.Path,
            "v-test-delete-tag",
            repository.HeadCommitHash,
            isAnnotated: false,
            sign: false,
            message: string.Empty,
            CancellationToken.None);

        await tagService.DeleteTagAsync(
            repository.Path,
            "v-test-delete-tag",
            CancellationToken.None);

        var tagRef = await repository.GitCliService.ExecuteBufferedAsync(
            ["show-ref", "--verify", "refs/tags/v-test-delete-tag"],
            repository.Path,
            validateExitCode: false,
            cancellationToken: CancellationToken.None);

        Assert.NotEqual(0, tagRef.ExitCode);
    }

    [Fact]
    public async Task PushTagAsync_PushesTagToRemote()
    {
        using var repository = TemporaryTagGitRepository.Create();
        using var remote = TemporaryBareRepository.Create();
        var tagService = new GitTagCommandService(repository.GitOperationService);
        await repository.GitCliService.ExecuteBufferedAsync(
            ["remote", "add", "lovelygit-test", remote.Path],
            repository.Path,
            cancellationToken: CancellationToken.None);
        await tagService.CreateTagAsync(
            repository.Path,
            "v-test-push-tag",
            repository.HeadCommitHash,
            isAnnotated: false,
            sign: false,
            message: string.Empty,
            CancellationToken.None);

        await tagService.PushTagAsync(
            repository.Path,
            "lovelygit-test",
            "v-test-push-tag",
            CancellationToken.None);

        var tagRef = await repository.GitCliService.ExecuteBufferedAsync(
            ["show-ref", "--verify", "refs/tags/v-test-push-tag"],
            remote.Path,
            cancellationToken: CancellationToken.None);

        Assert.StartsWith(repository.HeadCommitHash, tagRef.StandardOutput);
    }

    [Fact]
    public async Task DeleteRemoteTagAsync_DeletesRemoteTagAndPreservesLocalTag()
    {
        using var repository = TemporaryTagGitRepository.Create();
        using var remote = TemporaryBareRepository.Create();
        var tagService = new GitTagCommandService(repository.GitOperationService);
        await repository.GitCliService.ExecuteBufferedAsync(
            ["remote", "add", "lovelygit-test", remote.Path],
            repository.Path,
            cancellationToken: CancellationToken.None);
        await tagService.CreateTagAsync(
            repository.Path,
            "v-test-remote-delete",
            repository.HeadCommitHash,
            false,
            false,
            null,
            CancellationToken.None);
        await tagService.PushTagAsync(
            repository.Path,
            "lovelygit-test",
            "v-test-remote-delete",
            CancellationToken.None);

        await tagService.DeleteRemoteTagAsync(
            repository.Path,
            "lovelygit-test",
            "v-test-remote-delete",
            CancellationToken.None);

        var remoteRef = await repository.GitCliService.ExecuteBufferedAsync(
            ["show-ref", "--verify", "refs/tags/v-test-remote-delete"],
            remote.Path,
            validateExitCode: false,
            cancellationToken: CancellationToken.None);
        var localRef = await repository.GitCliService.ExecuteBufferedAsync(
            ["show-ref", "--verify", "refs/tags/v-test-remote-delete"],
            repository.Path,
            cancellationToken: CancellationToken.None);
        Assert.NotEqual(0, remoteRef.ExitCode);
        Assert.StartsWith(repository.HeadCommitHash, localRef.StandardOutput);
    }

    [Theory]
    [InlineData("bad remote", "v-test")]
    [InlineData("origin", "bad tag")]
    public async Task DeleteRemoteTagAsync_InvalidInputDoesNotMutateRepository(
        string remoteName,
        string tagName)
    {
        var directory = Directory.CreateTempSubdirectory("lovelygit-invalid-remote-tag-");
        var sentinel = Path.Combine(directory.FullName, "sentinel.txt");
        await File.WriteAllTextAsync(sentinel, "unchanged");

        try
        {
            var service = new GitTagCommandService(
                new GitOperationService(new GitCliService()));
            await Assert.ThrowsAsync<ArgumentException>(() => service.DeleteRemoteTagAsync(
                directory.FullName,
                remoteName,
                tagName,
                CancellationToken.None));

            Assert.Equal("unchanged", await File.ReadAllTextAsync(sentinel));
            Assert.False(Directory.Exists(Path.Combine(directory.FullName, ".git")));
        }
        finally
        {
            directory.Delete(recursive: true);
        }
    }

}
