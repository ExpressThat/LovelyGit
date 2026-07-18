using System.Reflection;
using ExpressThat.LovelyGit.Services.Git.Cli;
using ExpressThat.LovelyGit.Services.Git.CommitGraph;
using ExpressThat.LovelyGit.Services.Git.CommitGraph.Queries;

namespace LovelyGit.Tests.Git.CommitGraph;

public sealed class CommitGraphPageServiceLifetimeTests
{
    [Fact]
    public async Task IdleClose_KeepsTheVisibleRepositoryTraversalAlive()
    {
        using var repository = TemporaryGitRepository.Create();
        var repositoryId = Guid.NewGuid();
        var open = await CommitGraphManager.TryOpenAsync(
            repository.Path,
            repositoryId,
            null!,
            CancellationToken.None);
        var graph = Assert.IsType<CommitGraphManager>(open.Graph);
        using var service = CreateService(TimeSpan.FromMilliseconds(40));
        var activeGraphs = GetActiveGraphs(service);
        var cacheWorkLock = GetCacheWorkLock(service);
        activeGraphs.Add(repositoryId, graph);
        SetActiveRepository(service, repositoryId);

        service.ScheduleGraphClose(repositoryId);
        await Task.Delay(120);

        Assert.True(ContainsGraph(cacheWorkLock, activeGraphs, repositoryId));
        var page = await graph.GetCommitGraphPageAsync(
            new CommitGraphCursorState(null, 0),
            1,
            CancellationToken.None);
        Assert.Single(page.Response.Rows);
    }

    [Fact]
    public async Task IdleClose_CanBeCancelledAndEventuallyDisposesGraph()
    {
        using var repository = TemporaryGitRepository.Create();
        var repositoryId = Guid.NewGuid();
        var open = await CommitGraphManager.TryOpenAsync(
            repository.Path,
            repositoryId,
            null!,
            CancellationToken.None);
        var graph = Assert.IsType<CommitGraphManager>(open.Graph);
        using var service = CreateService(TimeSpan.FromMilliseconds(40));
        var activeGraphs = GetActiveGraphs(service);
        var cacheWorkLock = GetCacheWorkLock(service);
        activeGraphs.Add(repositoryId, graph);

        service.ScheduleGraphClose(repositoryId);
        service.CancelScheduledGraphClose(repositoryId);
        await Task.Delay(120);

        Assert.True(ContainsGraph(cacheWorkLock, activeGraphs, repositoryId));

        service.ScheduleGraphClose(repositoryId);
        await WaitForAsync(() => !ContainsGraph(cacheWorkLock, activeGraphs, repositoryId));

        await Assert.ThrowsAsync<ObjectDisposedException>(() =>
            graph.GetCommitGraphPageAsync(
                new CommitGraphCursorState(null, 0),
                1,
                CancellationToken.None));
    }

    [Fact]
    public async Task ClearingTheActiveRepository_DisposesItsTraversalImmediately()
    {
        using var repository = TemporaryGitRepository.Create();
        var repositoryId = Guid.NewGuid();
        var open = await CommitGraphManager.TryOpenAsync(
            repository.Path,
            repositoryId,
            null!,
            CancellationToken.None);
        var graph = Assert.IsType<CommitGraphManager>(open.Graph);
        using var service = CreateService(TimeSpan.FromSeconds(30));
        var activeGraphs = GetActiveGraphs(service);
        var cacheWorkLock = GetCacheWorkLock(service);
        activeGraphs.Add(repositoryId, graph);
        SetActiveRepository(service, repositoryId);

        await service.SwitchActiveRepositoryAsync(null);

        Assert.False(ContainsGraph(cacheWorkLock, activeGraphs, repositoryId));
        await Assert.ThrowsAsync<ObjectDisposedException>(() =>
            graph.GetCommitGraphPageAsync(
                new CommitGraphCursorState(null, 0),
                1,
                CancellationToken.None));
    }

    private static CommitGraphPageService CreateService(TimeSpan closeDelay) =>
        new(
            null!,
            null!,
            new CommitDetailsPreloadService(null!, null!),
            new CommitFileDiffService(null!),
            new CommitGraphBackgroundWorkerOptions(false, false, false),
            closeDelay);

    private static Dictionary<Guid, CommitGraphManager> GetActiveGraphs(
        CommitGraphPageService service)
    {
        var field = typeof(CommitGraphPageService).GetField(
            "_activeGraphs",
            BindingFlags.Instance | BindingFlags.NonPublic);
        return Assert.IsType<Dictionary<Guid, CommitGraphManager>>(field?.GetValue(service));
    }

    private static object GetCacheWorkLock(CommitGraphPageService service)
    {
        var field = typeof(CommitGraphPageService).GetField(
            "_cacheWorkLock",
            BindingFlags.Instance | BindingFlags.NonPublic);
        return Assert.IsType<object>(field?.GetValue(service));
    }

    private static void SetActiveRepository(CommitGraphPageService service, Guid repositoryId)
    {
        var field = typeof(CommitGraphPageService).GetField(
            "_activeRepositoryId",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(field);
        field.SetValue(service, repositoryId);
    }

    private static bool ContainsGraph(
        object cacheWorkLock,
        Dictionary<Guid, CommitGraphManager> activeGraphs,
        Guid repositoryId)
    {
        lock (cacheWorkLock)
        {
            return activeGraphs.ContainsKey(repositoryId);
        }
    }

    private static async Task WaitForAsync(Func<bool> condition)
    {
        var timeout = DateTime.UtcNow.AddSeconds(2);
        while (!condition() && DateTime.UtcNow < timeout)
        {
            await Task.Delay(10);
        }

        Assert.True(condition());
    }

    private sealed class TemporaryGitRepository : IDisposable
    {
        private readonly DirectoryInfo _directory;

        private TemporaryGitRepository(DirectoryInfo directory)
        {
            _directory = directory;
            Path = directory.FullName;
        }

        public string Path { get; }

        public static TemporaryGitRepository Create()
        {
            var directory = Directory.CreateTempSubdirectory("lovelygit-idle-graph-");
            var git = new GitCliService();
            RunGit(git, directory.FullName, ["init"]);
            RunGit(git, directory.FullName, ["config", "user.name", "LovelyGit Test"]);
            RunGit(git, directory.FullName, ["config", "user.email", "test@example.invalid"]);
            RunGit(git, directory.FullName, ["commit", "--allow-empty", "-m", "Initial"]);
            return new TemporaryGitRepository(directory);
        }

        public void Dispose()
        {
            foreach (var file in _directory.EnumerateFiles("*", SearchOption.AllDirectories))
            {
                file.Attributes = FileAttributes.Normal;
            }

            _directory.Delete(recursive: true);
        }

        private static void RunGit(
            GitCliService git,
            string workingDirectory,
            IReadOnlyList<string> arguments)
        {
            _ = git.ExecuteBufferedAsync(arguments, workingDirectory).GetAwaiter().GetResult();
        }
    }
}
