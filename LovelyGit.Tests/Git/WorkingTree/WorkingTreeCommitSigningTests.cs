using CliWrap;
using CliWrap.Buffered;
using ExpressThat.LovelyGit.Services.Git.WorkingTree;
using LovelyGit.Tests.Git.Branches;

namespace LovelyGit.Tests.Git.WorkingTree;

[Collection(GitShellIntegrationCollection.Name)]
public sealed class WorkingTreeCommitSigningTests
{
    [Fact]
    public async Task CommitStagedChangesAsync_CreatesSshSignedCommit()
    {
        using var repository = TemporaryGitRepository.Create();
        await ConfigureSshSigningAsync(repository);
        await StageFileAsync(repository, "signed.txt", "signed content");

        await CreateService(repository).CommitStagedChangesAsync(
            repository.Path,
            "Signed commit",
            "Signed body",
            amend: false,
            sign: true,
            CancellationToken.None);

        var commit = await GitOutputAsync(repository, "cat-file", "-p", "HEAD");
        Assert.Contains("gpgsig -----BEGIN SSH SIGNATURE-----", commit, StringComparison.Ordinal);
        Assert.Equal("2", await GitOutputAsync(repository, "rev-list", "--count", "HEAD"));
    }

    [Fact]
    public async Task CommitStagedChangesAsync_SignsAmendedCommitWithoutStagedChanges()
    {
        using var repository = TemporaryGitRepository.Create();
        await ConfigureSshSigningAsync(repository);
        var originalHead = await GitOutputAsync(repository, "rev-parse", "HEAD");

        await CreateService(repository).CommitStagedChangesAsync(
            repository.Path,
            "Signed amendment",
            string.Empty,
            amend: true,
            sign: true,
            CancellationToken.None);

        var amendedHead = await GitOutputAsync(repository, "rev-parse", "HEAD");
        var commit = await GitOutputAsync(repository, "cat-file", "-p", "HEAD");
        Assert.NotEqual(originalHead, amendedHead);
        Assert.Contains("gpgsig -----BEGIN SSH SIGNATURE-----", commit, StringComparison.Ordinal);
        Assert.Equal("1", await GitOutputAsync(repository, "rev-list", "--count", "HEAD"));
    }

    [Fact]
    public async Task CommitStagedChangesAsync_SigningFailurePreservesStateAndAllowsUnsignedRetry()
    {
        using var repository = TemporaryGitRepository.Create();
        await repository.GitCliService.ExecuteBufferedAsync(
            ["config", "gpg.format", "ssh"], repository.Path);
        await repository.GitCliService.ExecuteBufferedAsync(
            ["config", "user.signingkey", Path.Combine(repository.Path, "missing-key.pub")],
            repository.Path);
        await StageFileAsync(repository, "retry.txt", "retry content");
        var head = await GitOutputAsync(repository, "rev-parse", "HEAD");
        var indexTree = await GitOutputAsync(repository, "write-tree");
        var status = await GitOutputAsync(repository, "status", "--porcelain=v1");

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            CreateService(repository).CommitStagedChangesAsync(
                repository.Path,
                "Retry commit",
                string.Empty,
                amend: false,
                sign: true,
                CancellationToken.None));

        Assert.Contains("could not sign", error.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(head, await GitOutputAsync(repository, "rev-parse", "HEAD"));
        Assert.Equal(indexTree, await GitOutputAsync(repository, "write-tree"));
        Assert.Equal(status, await GitOutputAsync(repository, "status", "--porcelain=v1"));

        await CreateService(repository).CommitStagedChangesAsync(
            repository.Path,
            "Retry commit",
            string.Empty,
            amend: false,
            sign: false,
            CancellationToken.None);
        Assert.Equal("2", await GitOutputAsync(repository, "rev-list", "--count", "HEAD"));
    }

    [Fact]
    public async Task CommitStagedChangesAsync_CancelledSigningDoesNotMutateRepository()
    {
        using var repository = TemporaryGitRepository.Create();
        await StageFileAsync(repository, "cancelled.txt", "cancelled content");
        var head = await GitOutputAsync(repository, "rev-parse", "HEAD");
        var index = await File.ReadAllBytesAsync(Path.Combine(repository.Path, ".git", "index"));
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            CreateService(repository).CommitStagedChangesAsync(
                repository.Path,
                "Cancelled commit",
                string.Empty,
                amend: false,
                sign: true,
                cancellation.Token));

        Assert.Equal(head, await GitOutputAsync(repository, "rev-parse", "HEAD"));
        Assert.Equal(index, await File.ReadAllBytesAsync(Path.Combine(repository.Path, ".git", "index")));
    }

    private static WorkingTreeIndexService CreateService(TemporaryGitRepository repository) =>
        new(repository.GitCliService);

    private static async Task ConfigureSshSigningAsync(TemporaryGitRepository repository)
    {
        var executableName = OperatingSystem.IsWindows() ? "ssh-keygen.exe" : "ssh-keygen";
        var sshKeygen = repository.GitCliService.Installation.PathDirectories
            .Select(directory => Path.Combine(directory, executableName))
            .FirstOrDefault(File.Exists) ?? executableName;
        var keyPath = Path.Combine(repository.Path, "signing-key");
        await global::CliWrap.Cli.Wrap(sshKeygen)
            .WithArguments(["-q", "-t", "ed25519", "-N", "", "-C", "lovelygit-test", "-f", keyPath])
            .ExecuteBufferedAsync();
        await repository.GitCliService.ExecuteBufferedAsync(
            ["config", "gpg.format", "ssh"], repository.Path);
        await repository.GitCliService.ExecuteBufferedAsync(
            ["config", "user.signingkey", $"{keyPath}.pub"], repository.Path);
    }

    private static async Task StageFileAsync(
        TemporaryGitRepository repository,
        string path,
        string content)
    {
        await File.WriteAllTextAsync(Path.Combine(repository.Path, path), content);
        await repository.GitCliService.ExecuteBufferedAsync(["add", path], repository.Path);
    }

    private static async Task<string> GitOutputAsync(
        TemporaryGitRepository repository,
        params string[] arguments) =>
        (await repository.GitCliService.ExecuteBufferedAsync(arguments, repository.Path))
        .StandardOutput.Trim();
}
