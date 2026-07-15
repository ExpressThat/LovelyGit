using ExpressThat.LovelyGit.Services.Git.Bisect;
using ExpressThat.LovelyGit.Services.Git.Cli;
using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser;
using LovelyGit.Tests.Git.WorkingTree;

namespace LovelyGit.Tests.Git.Bisect;

[Collection(GitShellIntegrationCollection.Name)]
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
    public async Task Start_InvalidTargetsAndCancellationLeaveRepositoryUnchanged()
    {
        using var repository = CreateRepository();
        var reader = new NativeGitBisectStateReader();
        var service = CreateService(reader);
        var head = repository.Commits[^1];
        var blobPath = Path.Combine(repository.Path, "not-commit.txt");
        await File.WriteAllTextAsync(blobPath, "blob");
        var blob = (await GitTestProcess.RunAsync(
            repository.Path, "hash-object", "-w", "--", blobPath)).Trim();

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.ExecuteAsync(
            repository.Path, GitBisectAction.Start, head, CancellationToken.None));
        await Assert.ThrowsAsync<ArgumentException>(() => service.ExecuteAsync(
            repository.Path, GitBisectAction.Start, blob, CancellationToken.None));
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => service.ExecuteAsync(
            repository.Path, GitBisectAction.Start, repository.Commits[0], cancellation.Token));

        Assert.False((await reader.ReadAsync(repository.Path, CancellationToken.None)).IsActive);
        Assert.Equal(head, (await GitTestProcess.RunAsync(repository.Path, "rev-parse", "HEAD")).Trim());
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

    [Fact]
    public async Task Reader_UsesLinkedWorktreeBisectRefsInsteadOfCommonRefs()
    {
        using var repository = CreateRepository();
        var worktreePath = Path.Combine(Path.GetTempPath(), $"lovelygit-bisect-linked-{Guid.NewGuid():N}");
        try
        {
            await GitTestProcess.RunAsync(
                repository.Path, "worktree", "add", "-b", "linked-bisect", worktreePath, "main");
            var paths = await GitRepositoryDiscovery.ResolveRepositoryPathsAsync(
                worktreePath, CancellationToken.None);
            SeedBisectRefs(paths.WorktreeGitDirectory, repository.Commits[1], repository.Commits[0]);
            var commonRefs = Directory.CreateDirectory(
                Path.Combine(paths.GitDirectory, "refs", "bisect"));
            await File.WriteAllTextAsync(
                Path.Combine(commonRefs.FullName, "bad"), repository.Commits[^1] + "\n");

            var state = await new NativeGitBisectStateReader().ReadAsync(
                worktreePath, CancellationToken.None);

            Assert.True(state.IsActive);
            Assert.Equal(repository.Commits[1], state.BadCommit);
            Assert.Equal([repository.Commits[0]], state.GoodCommits);
            var startPath = Path.Combine(paths.WorktreeGitDirectory, "BISECT_START");
            var startBefore = await File.ReadAllBytesAsync(startPath);
            using var cancellation = new CancellationTokenSource();
            cancellation.Cancel();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
                new NativeGitBisectStateReader().ReadAsync(worktreePath, cancellation.Token));
            Assert.Equal(startBefore, await File.ReadAllBytesAsync(startPath));
        }
        finally
        {
            await GitTestProcess.RunAsync(repository.Path, "worktree", "remove", "--force", worktreePath);
            if (Directory.Exists(worktreePath)) Directory.Delete(worktreePath, recursive: true);
        }
    }

    private static void SeedBisectRefs(string gitDirectory, string bad, string good)
    {
        var refs = Directory.CreateDirectory(Path.Combine(gitDirectory, "refs", "bisect"));
        File.WriteAllText(Path.Combine(refs.FullName, "bad"), bad + "\n");
        File.WriteAllText(Path.Combine(refs.FullName, $"good-{good}"), good + "\n");
        File.WriteAllText(Path.Combine(refs.FullName, "good-malformed"), "not-an-object-id\n");
        File.WriteAllText(Path.Combine(gitDirectory, "BISECT_START"), "linked-bisect\n");
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
