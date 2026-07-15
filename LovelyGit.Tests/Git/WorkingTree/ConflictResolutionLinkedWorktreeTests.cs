using ExpressThat.LovelyGit.Services.Git.Cli;
using ExpressThat.LovelyGit.Services.Git.CommitGraph.Models;
using ExpressThat.LovelyGit.Services.Git.WorkingTree;
using LovelyGit.Tests.Git.Branches;

namespace LovelyGit.Tests.Git.WorkingTree;

public sealed class ConflictResolutionLinkedWorktreeTests
{
    [Fact]
    public async Task ExternalMergeTool_UsesTheLinkedWorktreeConflictIndex()
    {
        using var repository = TemporaryGitRepository.Create();
        var linkedPath = repository.Path + "-linked-tool";
        try
        {
            await CreateLinkedConflictAsync(repository, linkedPath);
            var runner = new StubMergeToolRunner(async (root, path, cancellationToken) =>
            {
                await File.WriteAllTextAsync(
                    Path.Combine(root, path), "resolved\n", cancellationToken);
                await repository.GitCliService.ExecuteBufferedAsync(
                    ["add", "--", path], root, cancellationToken: cancellationToken);
                return new ConflictMergeToolResult(0, string.Empty, string.Empty);
            });

            await new ConflictExternalMergeToolService(runner).OpenAsync(
                linkedPath, "shared.txt", CancellationToken.None);

            var unmerged = await repository.GitCliService.ExecuteBufferedAsync(
                ["ls-files", "--unmerged"], linkedPath);
            Assert.Equal(string.Empty, unmerged.StandardOutput);
            Assert.Equal("resolved\n", await File.ReadAllTextAsync(
                Path.Combine(linkedPath, "shared.txt")));
        }
        finally
        {
            await RemoveLinkedWorktreeAsync(repository, linkedPath);
        }
    }

    [Fact]
    public async Task ReadAsync_UsesTheLinkedWorktreeConflictIndex()
    {
        using var repository = TemporaryGitRepository.Create();
        var linkedPath = repository.Path + "-linked-conflict";
        try
        {
            await CreateLinkedConflictAsync(repository, linkedPath);

            var response = await new ConflictResolutionService(
                    new WorkingTreeIndexService(repository.GitCliService))
                .ReadAsync(
                    linkedPath,
                    "shared.txt",
                    CommitDiffViewMode.SideBySide,
                    ignoreWhitespace: false,
                    CancellationToken.None);

            Assert.Equal("base\n", response.Base.Text);
            Assert.Equal("current\n", response.Ours.Text);
            Assert.Equal("incoming\n", response.Theirs.Text);
            Assert.Equal("current", response.CurrentSource.RefName);
            Assert.Equal("incoming", response.IncomingSource.RefName);
        }
        finally
        {
            await RemoveLinkedWorktreeAsync(repository, linkedPath);
        }
    }

    private static async Task CreateLinkedConflictAsync(
        TemporaryGitRepository repository,
        string linkedPath)
    {
        await WriteAndCommitAsync(repository, repository.Path, "base\n", "base");
        await RunAsync(repository, repository.Path, "checkout", "-b", "incoming");
        await WriteAndCommitAsync(repository, repository.Path, "incoming\n", "incoming");
        await RunAsync(repository, repository.Path, "checkout", "master");
        await RunAsync(repository, repository.Path, "branch", "current");
        await RunAsync(repository, repository.Path, "worktree", "add", linkedPath, "current");
        await WriteAndCommitAsync(repository, linkedPath, "current\n", "current");
        var merge = await repository.GitCliService.ExecuteBufferedAsync(
            ["merge", "incoming"], linkedPath, validateExitCode: false,
            cancellationToken: CancellationToken.None);
        Assert.NotEqual(0, merge.ExitCode);
    }

    private static async Task RemoveLinkedWorktreeAsync(
        TemporaryGitRepository repository,
        string linkedPath)
    {
        if (!Directory.Exists(linkedPath)) return;
        _ = await repository.GitCliService.ExecuteBufferedAsync(
            ["worktree", "remove", "--force", linkedPath], repository.Path,
            validateExitCode: false, cancellationToken: CancellationToken.None);
        if (Directory.Exists(linkedPath))
        {
            TemporaryGitDirectory.Delete(new DirectoryInfo(linkedPath));
        }
    }

    private static async Task WriteAndCommitAsync(
        TemporaryGitRepository repository,
        string path,
        string content,
        string subject)
    {
        await File.WriteAllTextAsync(Path.Combine(path, "shared.txt"), content);
        await RunAsync(repository, path, "add", "shared.txt");
        await RunAsync(repository, path, "commit", "-m", subject);
    }

    private static async Task RunAsync(
        TemporaryGitRepository repository,
        string path,
        params string[] arguments) =>
        _ = await repository.GitCliService.ExecuteBufferedAsync(
            arguments, path, cancellationToken: CancellationToken.None);

    private sealed class StubMergeToolRunner(
        Func<string, string, CancellationToken, Task<ConflictMergeToolResult>> run)
        : IConflictMergeToolRunner
    {
        public Task<ConflictMergeToolResult> RunAsync(
            string repositoryPath,
            string path,
            CancellationToken cancellationToken) => run(repositoryPath, path, cancellationToken);
    }
}
