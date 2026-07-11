using CliWrap.Buffered;
using ExpressThat.LovelyGit.Services.Git.Cli;
using ExpressThat.LovelyGit.Services.Git.Tags;
using LovelyGit.Tests.Git.Branches;

namespace LovelyGit.Tests.Git.Tags;

public sealed class GitSignedTagCommandServiceTests
{
    [Fact]
    public async Task CreateTagAsync_CreatesSshSignedAnnotatedTag()
    {
        using var repository = TemporaryGitRepository.Create();
        await ConfigureSshSigningAsync(repository);

        await CreateService(repository).CreateTagAsync(
            repository.Path,
            "v-signed",
            repository.HeadCommitHash,
            isAnnotated: true,
            sign: true,
            message: "Signed release",
            CancellationToken.None);

        var contents = await GitOutputAsync(repository, "cat-file", "-p", "refs/tags/v-signed");
        Assert.Contains("Signed release", contents, StringComparison.Ordinal);
        Assert.Contains("BEGIN SSH SIGNATURE", contents, StringComparison.Ordinal);
    }

    [Fact]
    public async Task CreateTagAsync_SigningFailureLeavesNoTagAndAllowsRetry()
    {
        using var repository = TemporaryGitRepository.Create();
        await repository.GitCliService.ExecuteBufferedAsync(
            ["config", "user.signingkey", "missing-signing-key"], repository.Path);
        var service = CreateService(repository);

        await Assert.ThrowsAsync<GitOperationException>(() => service.CreateTagAsync(
            repository.Path,
            "v-retry",
            repository.HeadCommitHash,
            isAnnotated: true,
            sign: true,
            message: "Release",
            CancellationToken.None));

        Assert.False(await TagExistsAsync(repository, "v-retry"));
        await service.CreateTagAsync(
            repository.Path,
            "v-retry",
            repository.HeadCommitHash,
            isAnnotated: true,
            sign: false,
            message: "Release",
            CancellationToken.None);
        Assert.True(await TagExistsAsync(repository, "v-retry"));
    }

    [Fact]
    public async Task CreateTagAsync_RejectsSigningALightweightTagWithoutMutation()
    {
        using var repository = TemporaryGitRepository.Create();

        var error = await Assert.ThrowsAsync<ArgumentException>(() =>
            CreateService(repository).CreateTagAsync(
                repository.Path,
                "v-invalid",
                repository.HeadCommitHash,
                isAnnotated: false,
                sign: true,
                message: null,
                CancellationToken.None));

        Assert.Contains("annotated", error.Message, StringComparison.OrdinalIgnoreCase);
        Assert.False(await TagExistsAsync(repository, "v-invalid"));
    }

    [Fact]
    public async Task CreateTagAsync_CancelledSigningLeavesNoTag()
    {
        using var repository = TemporaryGitRepository.Create();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            CreateService(repository).CreateTagAsync(
                repository.Path,
                "v-cancelled",
                repository.HeadCommitHash,
                isAnnotated: true,
                sign: true,
                message: "Release",
                cancellation.Token));

        Assert.False(await TagExistsAsync(repository, "v-cancelled"));
    }

    private static GitTagCommandService CreateService(TemporaryGitRepository repository) =>
        new(new GitOperationService(repository.GitCliService));

    private static async Task ConfigureSshSigningAsync(TemporaryGitRepository repository)
    {
        var executable = OperatingSystem.IsWindows() ? "ssh-keygen.exe" : "ssh-keygen";
        var sshKeygen = repository.GitCliService.Installation.PathDirectories
            .Select(directory => Path.Combine(directory, executable))
            .FirstOrDefault(File.Exists) ?? executable;
        var keyPath = Path.Combine(repository.Path, "signing-key");
        await global::CliWrap.Cli.Wrap(sshKeygen)
            .WithArguments(["-q", "-t", "ed25519", "-N", "", "-f", keyPath])
            .ExecuteBufferedAsync();
        await repository.GitCliService.ExecuteBufferedAsync(
            ["config", "gpg.format", "ssh"], repository.Path);
        await repository.GitCliService.ExecuteBufferedAsync(
            ["config", "user.signingkey", $"{keyPath}.pub"], repository.Path);
    }

    private static async Task<bool> TagExistsAsync(
        TemporaryGitRepository repository,
        string name) =>
        (await repository.GitCliService.ExecuteBufferedAsync(
            ["show-ref", "--verify", $"refs/tags/{name}"],
            repository.Path,
            validateExitCode: false)).ExitCode == 0;

    private static async Task<string> GitOutputAsync(
        TemporaryGitRepository repository,
        params string[] arguments) =>
        (await repository.GitCliService.ExecuteBufferedAsync(arguments, repository.Path))
        .StandardOutput;
}
