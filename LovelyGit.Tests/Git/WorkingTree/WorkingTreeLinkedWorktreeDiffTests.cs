using ExpressThat.LovelyGit.Services.Git.CommitGraph.Models;
using ExpressThat.LovelyGit.Services.Git.WorkingTree;
using ExpressThat.LovelyGit.Services.Git.WorkingTree.Models;
using LovelyGit.Tests.Git.Branches;

namespace LovelyGit.Tests.Git.WorkingTree;

public sealed class WorkingTreeLinkedWorktreeDiffTests
{
    [Fact]
    public async Task GetFileDiffAsync_UsesTheLinkedWorktreeIndex()
    {
        using var repository = TemporaryGitRepository.Create();
        var linkedPath = repository.Path + "-linked-diff";
        try
        {
            await WriteAndCommitAsync(repository, repository.Path, "main version\n", "main file");
            await RunAsync(repository, repository.Path, "checkout", "-b", "feature/diff");
            await WriteAndCommitAsync(repository, repository.Path, "feature version\n", "feature file");
            await RunAsync(repository, repository.Path, "checkout", "master");
            await RunAsync(
                repository, repository.Path, "worktree", "add", linkedPath, "feature/diff");
            await File.WriteAllTextAsync(
                Path.Combine(linkedPath, "tracked.txt"), "feature modified\n");

            var response = await new WorkingTreeChangeService().GetFileDiffAsync(
                linkedPath,
                "tracked.txt",
                WorkingTreeChangeGroup.Unstaged,
                CommitDiffViewMode.Combined,
                ignoreWhitespace: false,
                CancellationToken.None);

            Assert.Contains(
                response.Lines,
                line => line.ChangeType == "Deleted" && line.Text == "feature version");
            Assert.DoesNotContain(
                response.Lines,
                line => line.ChangeType == "Deleted" && line.Text == "main version");

            await RunAsync(repository, linkedPath, "add", "tracked.txt");
            var staged = await new WorkingTreeChangeService().GetFileDiffAsync(
                linkedPath,
                "tracked.txt",
                WorkingTreeChangeGroup.Staged,
                CommitDiffViewMode.Combined,
                ignoreWhitespace: false,
                CancellationToken.None);
            Assert.Contains(
                staged.Lines,
                line => line.ChangeType == "Deleted" && line.Text == "feature version");
            Assert.Contains(
                staged.Lines,
                line => line.ChangeType == "Inserted" && line.Text == "feature modified");
        }
        finally
        {
            if (Directory.Exists(linkedPath))
            {
                await RunAsync(
                    repository,
                    repository.Path,
                    "worktree",
                    "remove",
                    "--force",
                    linkedPath);
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
        await File.WriteAllTextAsync(Path.Combine(path, "tracked.txt"), content);
        await RunAsync(repository, path, "add", "tracked.txt");
        await RunAsync(repository, path, "commit", "-m", subject);
    }

    private static async Task RunAsync(
        TemporaryGitRepository repository,
        string path,
        params string[] arguments) =>
        _ = await repository.GitCliService.ExecuteBufferedAsync(
            arguments, path, cancellationToken: CancellationToken.None);
}
