using ExpressThat.LovelyGit.Services.Git.Cli;
using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser.Operations;

namespace LovelyGit.Tests.Git.BranchIntegration;

public sealed class GitBranchIntegrationServiceTests
{
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

    private sealed class TestRepository : IDisposable
    {
        private readonly DirectoryInfo _directory;

        private TestRepository(DirectoryInfo directory, GitCliService git)
        {
            _directory = directory;
            Git = git;
            Path = directory.FullName;
            Service = new GitBranchIntegrationService(new GitOperationService(git));
        }

        public GitCliService Git { get; }

        public string Path { get; }

        public GitBranchIntegrationService Service { get; }

        public static TestRepository Create()
        {
            var directory = Directory.CreateTempSubdirectory("lovelygit-integration-");
            var repository = new TestRepository(directory, new GitCliService());
            repository.RunAsync("init", "--initial-branch=main").GetAwaiter().GetResult();
            repository.RunAsync("config", "user.name", "LovelyGit Test").GetAwaiter().GetResult();
            repository.RunAsync("config", "user.email", "test@example.invalid").GetAwaiter().GetResult();
            File.WriteAllText(System.IO.Path.Combine(repository.Path, "shared.txt"), "base");
            repository.RunAsync("add", ".").GetAwaiter().GetResult();
            repository.RunAsync("commit", "-m", "initial").GetAwaiter().GetResult();
            return repository;
        }

        public async Task CommitFileAsync(
            string relativePath,
            string content,
            string message)
        {
            await File.WriteAllTextAsync(System.IO.Path.Combine(Path, relativePath), content);
            await RunAsync("add", "--", relativePath);
            await RunAsync("commit", "-m", message);
        }

        public async Task CreateBranchCommitAsync(
            string branchName,
            string relativePath,
            string content)
        {
            await RunAsync("switch", "--create", branchName);
            await CommitFileAsync(relativePath, content, $"{branchName} change");
        }

        public Task SwitchAsync(string branchName) => RunAsync("switch", branchName);

        public Task RunGitAsync(params string[] arguments) => RunAsync(arguments);

        public void Dispose()
        {
            foreach (var file in _directory.EnumerateFiles("*", SearchOption.AllDirectories))
            {
                file.Attributes = FileAttributes.Normal;
            }

            _directory.Delete(recursive: true);
        }

        private async Task RunAsync(params string[] arguments)
        {
            await Git.ExecuteBufferedAsync(
                arguments,
                Path,
                cancellationToken: CancellationToken.None);
        }
    }
}
