using ExpressThat.LovelyGit.Services.Git.Cli;
using ExpressThat.LovelyGit.Services.Git.CommitGraph.Models;
using ExpressThat.LovelyGit.Services.Git.WorkingTree;
using LovelyGit.Tests.Git.Branches;

namespace LovelyGit.Tests.Git.WorkingTree;

public sealed class ConflictResolutionLinkedWorktreeTests
{
    [Fact]
    public async Task ReadAsync_UsesTheLinkedWorktreeConflictIndex()
    {
        using var repository = TemporaryGitRepository.Create();
        var linkedPath = repository.Path + "-linked-conflict";
        try
        {
            await WriteAndCommitAsync(repository, repository.Path, "base\n", "base");
            await RunAsync(repository, repository.Path, "checkout", "-b", "incoming");
            await WriteAndCommitAsync(repository, repository.Path, "incoming\n", "incoming");
            await RunAsync(repository, repository.Path, "checkout", "master");
            await RunAsync(repository, repository.Path, "branch", "current");
            await RunAsync(
                repository, repository.Path, "worktree", "add", linkedPath, "current");
            await WriteAndCommitAsync(repository, linkedPath, "current\n", "current");
            var merge = await repository.GitCliService.ExecuteBufferedAsync(
                ["merge", "incoming"],
                linkedPath,
                validateExitCode: false,
                cancellationToken: CancellationToken.None);
            Assert.NotEqual(0, merge.ExitCode);

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
            if (Directory.Exists(linkedPath))
            {
                _ = await repository.GitCliService.ExecuteBufferedAsync(
                    ["worktree", "remove", "--force", linkedPath],
                    repository.Path,
                    validateExitCode: false,
                    cancellationToken: CancellationToken.None);
                if (Directory.Exists(linkedPath))
                {
                    TemporaryGitDirectory.Delete(new DirectoryInfo(linkedPath));
                }
            }
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
}
