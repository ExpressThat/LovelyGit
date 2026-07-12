using ExpressThat.LovelyGit.Services.Git.Cli;
using ExpressThat.LovelyGit.Services.Git.Reset;
using ExpressThat.LovelyGit.Services.Git.WorkingTree.Models;

namespace LovelyGit.Tests.Git.Reset;

public sealed class GitResetCommandServiceTests
{
    [Theory]
    [InlineData(GitResetMode.Soft, true, false, "after")]
    [InlineData(GitResetMode.Mixed, false, true, "after")]
    [InlineData(GitResetMode.Hard, false, false, "before")]
    public async Task ResetCurrentBranchToCommitAsync_PreservesExpectedState(
        GitResetMode mode,
        bool expectStagedChange,
        bool expectWorkingChange,
        string expectedContent)
    {
        using var repository = TemporaryGitRepository.Create();

        await repository.Service.ResetCurrentBranchToCommitAsync(
            repository.Path,
            repository.FirstCommitHash,
            mode,
            CancellationToken.None);

        Assert.Equal(repository.FirstCommitHash, repository.RunGit(["rev-parse", "HEAD"]));
        Assert.Equal(expectStagedChange, repository.RunGit(["diff", "--cached", "--name-only"]) != string.Empty);
        Assert.Equal(expectWorkingChange, repository.RunGit(["diff", "--name-only"]) != string.Empty);
        Assert.Equal(expectedContent, File.ReadAllText(Path.Combine(repository.Path, "file.txt")));
    }

    [Fact]
    public async Task ResetCurrentBranchToCommitAsync_RejectsDetachedHead()
    {
        using var repository = TemporaryGitRepository.Create();
        repository.RunGit(["switch", "--detach", repository.SecondCommitHash]);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            repository.Service.ResetCurrentBranchToCommitAsync(
                repository.Path,
                repository.FirstCommitHash,
                GitResetMode.Mixed,
                CancellationToken.None));

        Assert.Contains("local branch", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(repository.SecondCommitHash, repository.RunGit(["rev-parse", "HEAD"]));
    }

    [Fact]
    public async Task ResetCurrentBranchToCommitAsync_RejectsInvalidHashWithoutMutation()
    {
        var directory = Directory.CreateTempSubdirectory("lovelygit-invalid-reset-");
        var sentinel = Path.Combine(directory.FullName, "sentinel.txt");
        await File.WriteAllTextAsync(sentinel, "unchanged");

        try
        {
            var service = new GitResetCommandService(
                new GitOperationService(new GitCliService()));
            await Assert.ThrowsAsync<ArgumentException>(() =>
                service.ResetCurrentBranchToCommitAsync(
                    directory.FullName,
                    "not-a-hash",
                    GitResetMode.Mixed,
                    CancellationToken.None));

            Assert.Equal("unchanged", await File.ReadAllTextAsync(sentinel));
            Assert.False(Directory.Exists(Path.Combine(directory.FullName, ".git")));
        }
        finally
        {
            directory.Delete(recursive: true);
        }
    }

    [Fact]
    public async Task ResetCurrentBranchToCommitAsync_RejectsActiveRepositoryOperation()
    {
        using var repository = TemporaryGitRepository.Create();
        var mergeHeadPath = Path.Combine(repository.Path, ".git", "MERGE_HEAD");
        File.WriteAllText(mergeHeadPath, repository.FirstCommitHash);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            repository.Service.ResetCurrentBranchToCommitAsync(
                repository.Path,
                repository.FirstCommitHash,
                GitResetMode.Hard,
                CancellationToken.None));

        Assert.Contains("active merge", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(repository.SecondCommitHash, repository.RunGit(["rev-parse", "HEAD"]));
    }

    [Fact]
    public async Task UndoLastCommitAsync_AtomicallyRejectsAnUnexpectedCurrentHead()
    {
        using var repository = TemporaryGitRepository.Create();
        var index = File.ReadAllBytes(Path.Combine(repository.Path, ".git", "index"));

        await Assert.ThrowsAsync<GitOperationException>(() =>
            repository.Service.UndoLastCommitAsync(
                repository.Path,
                repository.FirstCommitHash,
                repository.FirstCommitHash,
                CancellationToken.None));

        Assert.Equal(repository.SecondCommitHash, repository.RunGit(["rev-parse", "HEAD"]));
        Assert.Equal(index, File.ReadAllBytes(Path.Combine(repository.Path, ".git", "index")));
        Assert.Equal("after", File.ReadAllText(Path.Combine(repository.Path, "file.txt")));
    }

    private sealed class TemporaryGitRepository : IDisposable
    {
        private static readonly RepositoryTemplate<bool> Template = new(
            "lovelygit-reset-template-",
            InitializeTemplate);
        private readonly DirectoryInfo _directory;
        private readonly GitCliService _git;

        private TemporaryGitRepository(DirectoryInfo directory, GitCliService git)
        {
            _directory = directory;
            _git = git;
            Path = directory.FullName;
            FirstCommitHash = RunGit(["rev-parse", "HEAD~1"]);
            SecondCommitHash = RunGit(["rev-parse", "HEAD"]);
            Service = new GitResetCommandService(new GitOperationService(git));
        }

        public string FirstCommitHash { get; }
        public string Path { get; }
        public GitResetCommandService Service { get; }
        public string SecondCommitHash { get; }

        public static TemporaryGitRepository Create()
        {
            var (directory, _) = Template.CreateCopy("lovelygit-reset-");
            var git = new GitCliService();
            return new TemporaryGitRepository(directory, git);
        }

        private static bool InitializeTemplate(DirectoryInfo directory)
        {
            var git = new GitCliService();
            InitializedRepositoryTemplate.CopyInto(directory);
            File.WriteAllText(System.IO.Path.Combine(directory.FullName, "file.txt"), "before");
            RunGit(git, directory.FullName, ["add", "file.txt"]);
            RunGit(git, directory.FullName, ["commit", "-m", "Before"]);
            File.WriteAllText(System.IO.Path.Combine(directory.FullName, "file.txt"), "after");
            RunGit(git, directory.FullName, ["commit", "-am", "After"]);
            return true;
        }

        public void Dispose()
        {
            foreach (var file in _directory.EnumerateFiles("*", SearchOption.AllDirectories))
            {
                file.Attributes = FileAttributes.Normal;
            }

            _directory.Delete(recursive: true);
        }

        public string RunGit(IReadOnlyList<string> arguments)
        {
            var result = _git.ExecuteBufferedAsync(arguments, Path).GetAwaiter().GetResult();
            return result.ExitCode == 0 ? result.StandardOutput.Trim() : result.StandardError.Trim();
        }

        private static void RunGit(
            GitCliService git,
            string workingDirectory,
            IReadOnlyList<string> arguments) =>
            git.ExecuteBufferedAsync(arguments, workingDirectory).GetAwaiter().GetResult();
    }
}
