using ExpressThat.LovelyGit.Services.Git.Bisect;
using ExpressThat.LovelyGit.Services.Git.Cli;
using LovelyGit.Tests.Git.WorkingTree;

namespace LovelyGit.Tests.Git.Bisect;

public sealed class GitBisectServicesTests
{
    [Fact]
    public async Task BisectWorkflow_FindsFirstBadCommitAndRestoresStartingBranch()
    {
        using var repository = CreateRepository();
        var commits = repository.Commits;
        var reader = new NativeGitBisectStateReader();
        var service = CreateService(reader);

        var state = await service.ExecuteAsync(
            repository.Path,
            GitBisectAction.Start,
            commits[0],
            CancellationToken.None);
        Assert.True(state.IsActive);
        Assert.Equal("main", state.StartingReference);

        for (var step = 0; step < 8 && state.FirstBadCommit == null; step++)
        {
            var currentIndex = commits.IndexOf(state.CurrentCommit!);
            Assert.True(currentIndex >= 0, $"Unexpected bisect commit {state.CurrentCommit}");
            state = await service.ExecuteAsync(
                repository.Path,
                currentIndex >= 2 ? GitBisectAction.MarkBad : GitBisectAction.MarkGood,
                goodCommit: null,
                CancellationToken.None);
        }

        Assert.Equal(commits[2], state.FirstBadCommit);
        Assert.Equal(commits[2], state.BadCommit);
        var reset = await service.ExecuteAsync(
            repository.Path,
            GitBisectAction.Reset,
            goodCommit: null,
            CancellationToken.None);
        Assert.False(reset.IsActive);
        Assert.Equal("main", (await GitTestProcess.RunAsync(
            repository.Path,
            "branch",
            "--show-current")).Trim());
    }

    [Fact]
    public async Task Start_RejectsUnknownGoodCommitWithoutChangingRepository()
    {
        using var repository = CreateRepository();
        var reader = new NativeGitBisectStateReader();

        await Assert.ThrowsAsync<ArgumentException>(() => CreateService(reader).ExecuteAsync(
            repository.Path,
            GitBisectAction.Start,
            "0".PadLeft(40, '0'),
            CancellationToken.None));

        Assert.False((await reader.ReadAsync(repository.Path, CancellationToken.None)).IsActive);
    }

    [Fact]
    public async Task Skip_MovesToAnotherCandidateWithoutEndingSession()
    {
        using var repository = CreateRepository();
        var commits = repository.Commits;
        var reader = new NativeGitBisectStateReader();
        var service = CreateService(reader);
        var started = await service.ExecuteAsync(
            repository.Path,
            GitBisectAction.Start,
            commits[0],
            CancellationToken.None);

        var skipped = await service.ExecuteAsync(
            repository.Path,
            GitBisectAction.Skip,
            goodCommit: null,
            CancellationToken.None);

        Assert.True(skipped.IsActive);
        Assert.NotEqual(started.CurrentCommit, skipped.CurrentCommit);
        await service.ExecuteAsync(
            repository.Path,
            GitBisectAction.Reset,
            goodCommit: null,
            CancellationToken.None);
    }

    private static GitBisectCommandService CreateService(NativeGitBisectStateReader reader) =>
        new(new GitOperationService(new GitCliService()), reader);

    private static BisectRepository CreateRepository() => BisectRepository.Create();

    private static async Task<List<string>> CreateHistoryAsync(string repositoryPath)
    {
        InitializedRepositoryTemplate.CopyInto(new DirectoryInfo(repositoryPath));
        var commits = new List<string>();
        for (var index = 0; index < 5; index++)
        {
            await File.WriteAllTextAsync(
                Path.Combine(repositoryPath, "behavior.txt"),
                $"{(index < 2 ? "good" : "bad")} revision {index}\n");
            await GitTestProcess.RunAsync(repositoryPath, "add", "behavior.txt");
            await GitTestProcess.RunAsync(repositoryPath, "commit", "-m", $"Revision {index + 1}");
            commits.Add((await GitTestProcess.RunAsync(repositoryPath, "rev-parse", "HEAD")).Trim());
        }

        return commits;
    }

    private sealed class BisectRepository : IDisposable
    {
        private static readonly RepositoryTemplate<List<string>> Template = new(
            "lovelygit-bisect-template-",
            directory => CreateHistoryAsync(directory.FullName).GetAwaiter().GetResult());
        private readonly DirectoryInfo _directory;

        private BisectRepository(DirectoryInfo directory, List<string> commits)
        {
            _directory = directory;
            Commits = commits;
        }

        public List<string> Commits { get; }
        public string Path => _directory.FullName;

        public static BisectRepository Create()
        {
            var (directory, commits) = Template.CreateCopy("lovelygit-bisect-");
            return new BisectRepository(directory, commits);
        }

        public void Dispose()
        {
            foreach (var file in _directory.EnumerateFiles("*", SearchOption.AllDirectories))
                file.Attributes = FileAttributes.Normal;
            _directory.Delete(recursive: true);
        }
    }
}
