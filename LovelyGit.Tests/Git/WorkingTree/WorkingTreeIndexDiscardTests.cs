using ExpressThat.LovelyGit.Services.Git.Cli;
using ExpressThat.LovelyGit.Services.Git.WorkingTree;
using ExpressThat.LovelyGit.Services.Git.WorkingTree.Models;

namespace LovelyGit.Tests.Git.WorkingTree;

public sealed class WorkingTreeIndexDiscardTests
{
    [Fact]
    public async Task DiscardChangesAsync_RestoresTrackedAndDeletesUntrackedFiles()
    {
        using var repository = TemporaryGitRepository.Create();
        var service = new WorkingTreeIndexService(repository.GitCliService);
        var trackedPath = System.IO.Path.Combine(repository.Path, "tracked.txt");
        var untrackedPath = System.IO.Path.Combine(repository.Path, "new.txt");
        await File.AppendAllTextAsync(trackedPath, "changed", CancellationToken.None);
        await File.WriteAllTextAsync(untrackedPath, "new", CancellationToken.None);

        await service.DiscardChangesAsync(
            repository.Path,
            [
                CreateFile("tracked.txt", WorkingTreeChangeGroup.Unstaged),
                CreateFile("new.txt", WorkingTreeChangeGroup.Untracked),
            ],
            CancellationToken.None);

        var status = await repository.GitCliService.ExecuteBufferedAsync(
            ["status", "--short"],
            repository.Path,
            cancellationToken: CancellationToken.None);

        Assert.Equal("tracked", await File.ReadAllTextAsync(trackedPath, CancellationToken.None));
        Assert.False(File.Exists(untrackedPath));
        Assert.Equal(string.Empty, status.StandardOutput.Trim());
    }

    [Fact]
    public async Task DiscardChangesAsync_RejectsStagedFiles()
    {
        using var repository = TemporaryGitRepository.Create();
        var service = new WorkingTreeIndexService(repository.GitCliService);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.DiscardChangesAsync(
                repository.Path,
                [CreateFile("tracked.txt", WorkingTreeChangeGroup.Staged)],
                CancellationToken.None));

        Assert.Contains("Only unstaged and untracked files", exception.Message);
    }

    private static WorkingTreeChangedFile CreateFile(
        string path,
        WorkingTreeChangeGroup group) =>
        new()
        {
            Group = group,
            Path = path,
            Status = group == WorkingTreeChangeGroup.Untracked ? "Added" : "Modified",
        };

    private sealed class TemporaryGitRepository : IDisposable
    {
        private readonly DirectoryInfo _directory;

        private TemporaryGitRepository(DirectoryInfo directory, GitCliService gitCliService)
        {
            _directory = directory;
            GitCliService = gitCliService;
            Path = directory.FullName;
        }

        public GitCliService GitCliService { get; }

        public string Path { get; }

        public static TemporaryGitRepository Create()
        {
            var directory = Directory.CreateTempSubdirectory("lovelygit-discard-");
            var gitCliService = new GitCliService();

            RunGit(gitCliService, directory.FullName, ["init"]);
            RunGit(gitCliService, directory.FullName, ["config", "user.name", "LovelyGit Test"]);
            RunGit(gitCliService, directory.FullName, ["config", "user.email", "test@example.invalid"]);
            File.WriteAllText(System.IO.Path.Combine(directory.FullName, "tracked.txt"), "tracked");
            RunGit(gitCliService, directory.FullName, ["add", "tracked.txt"]);
            RunGit(gitCliService, directory.FullName, ["commit", "-m", "Initial"]);

            return new TemporaryGitRepository(directory, gitCliService);
        }

        public void Dispose()
        {
            foreach (var file in _directory.EnumerateFiles("*", SearchOption.AllDirectories))
            {
                file.Attributes = FileAttributes.Normal;
            }

            _directory.Delete(recursive: true);
        }

        private static CliWrap.Buffered.BufferedCommandResult RunGit(
            GitCliService gitCliService,
            string workingDirectory,
            IReadOnlyList<string> arguments)
        {
            return gitCliService
                .ExecuteBufferedAsync(arguments, workingDirectory)
                .GetAwaiter()
                .GetResult();
        }
    }
}
