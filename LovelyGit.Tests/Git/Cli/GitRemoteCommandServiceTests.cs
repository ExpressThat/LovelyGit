using ExpressThat.LovelyGit.Services.Git.Cli;
using ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.WorkingTree;

namespace LovelyGit.Tests.Git.Cli;

[Collection(GitShellIntegrationCollection.Name)]
public sealed class GitRemoteCommandServiceTests
{
    [Theory]
    [InlineData(false, "fetch", "--all")]
    [InlineData(true, "fetch", "--all", "--prune")]
    public void BuildFetchArguments_UsesEveryConfiguredRemote(
        bool prune,
        params string[] expected)
    {
        Assert.Equal(expected, GitRemoteCommandService.BuildFetchArguments(null, prune));
    }

    [Fact]
    public void BuildFetchArguments_TargetsOneValidatedRemoteWhenRequested()
    {
        Assert.Equal(
            ["fetch", "--prune", "upstream"],
            GitRemoteCommandService.BuildFetchArguments("upstream", prune: true));
    }

    [Fact]
    public async Task FetchAsync_AllWithPrune_UpdatesEveryRemoteAndRemovesDeletedRefs()
    {
        using var repository = TemporaryRemoteGitRepository.Create();
        var service = new GitRemoteCommandService(repository.GitCliService);
        var backupPath = repository.CreateBareRemoteCopy("backup", "backup-only");
        repository.AddRemoteConfig("backup", backupPath);
        repository.WriteRemoteTrackingRef("origin", "obsolete");

        await service.FetchAsync(
            repository.ClonePath,
            remoteName: null,
            prune: true,
            CancellationToken.None);

        Assert.True(repository.HasRef(
            repository.ClonePath, "refs/remotes/backup/backup-only"));
        Assert.False(repository.HasRef(
            repository.ClonePath, "refs/remotes/origin/obsolete"));
    }

    [Fact]
    public void AddRemote_AppendsValidRemoteName()
    {
        var arguments = GitRemoteCommandService.AddRemote(["fetch"], "origin");

        Assert.Equal(["fetch", "origin"], arguments);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void AddRemote_LeavesArgumentsWithoutRemoteName(string? remoteName)
    {
        var arguments = GitRemoteCommandService.AddRemote(["pull", "--rebase"], remoteName);

        Assert.Equal(["pull", "--rebase"], arguments);
    }

    [Theory]
    [InlineData(" origin")]
    [InlineData("origin/main")]
    public void AddRemote_RejectsInvalidRemoteName(string remoteName)
    {
        Assert.Throws<ArgumentException>(() =>
            GitRemoteCommandService.AddRemote(["push"], remoteName));
    }

    [Fact]
    public async Task PullAsync_Rebase_ReplaysLocalCommitOnRemoteHead()
    {
        using var repository = TemporaryRemoteGitRepository.Create();
        var service = new GitRemoteCommandService(repository.GitCliService);
        repository.Commit(repository.ClonePath, "local.txt", "local change");
        repository.CommitAndPushFromUpdater("remote.txt", "remote change");

        await service.PullAsync(
            repository.ClonePath,
            GitPullMode.Rebase,
            remoteName: null,
            CancellationToken.None);

        var subjects = repository.RunGit(
                repository.ClonePath,
                ["log", "--format=%s", "-2"])
            .StandardOutput
            .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);

        Assert.Equal("local change", subjects[0]);
        Assert.Equal("remote change", subjects[1]);
    }

    [Fact]
    public async Task PullAsync_FastForwardOnly_RejectsDivergedBranches()
    {
        using var repository = TemporaryRemoteGitRepository.Create();
        var service = new GitRemoteCommandService(repository.GitCliService);
        repository.Commit(repository.ClonePath, "local.txt", "local change");
        repository.CommitAndPushFromUpdater("remote.txt", "remote change");

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.PullAsync(
                repository.ClonePath,
                GitPullMode.FastForwardOnly,
                remoteName: null,
                CancellationToken.None));
    }

    [Fact]
    public async Task PushAsync_ForceWithLease_ReplacesHistoryOnlyAfterNormalPushRejectsIt()
    {
        using var repository = TemporaryRemoteGitRepository.Create();
        var service = new GitRemoteCommandService(repository.GitCliService);
        repository.Commit(repository.ClonePath, "first.txt", "first local change");
        await service.PushAsync(
            repository.ClonePath,
            GitPushMode.Normal,
            remoteName: null,
            CancellationToken.None);
        repository.RunGit(repository.ClonePath, ["reset", "--hard", "HEAD~1"]);
        repository.Commit(repository.ClonePath, "replacement.txt", "replacement history");

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.PushAsync(
            repository.ClonePath,
            GitPushMode.Normal,
            remoteName: null,
            CancellationToken.None));
        await service.PushAsync(
            repository.ClonePath,
            GitPushMode.ForceWithLease,
            remoteName: null,
            CancellationToken.None);

        Assert.Equal(repository.Head(repository.ClonePath), repository.Head(repository.BarePath));
    }

    [Fact]
    public async Task PushAsync_ForceWithLease_RejectsRemoteWorkNotSeenLocally()
    {
        using var repository = TemporaryRemoteGitRepository.Create();
        var service = new GitRemoteCommandService(repository.GitCliService);
        repository.CommitAndPushFromUpdater("remote.txt", "remote work");
        repository.Commit(repository.ClonePath, "local.txt", "diverged local work");
        var remoteHead = repository.Head(repository.BarePath);

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => service.PushAsync(
            repository.ClonePath,
            GitPushMode.ForceWithLease,
            remoteName: null,
            CancellationToken.None));

        Assert.Contains("rejected", error.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(remoteHead, repository.Head(repository.BarePath));
    }

    [Fact]
    public async Task PushAsync_CancellationDoesNotChangeRemote()
    {
        using var repository = TemporaryRemoteGitRepository.Create();
        var service = new GitRemoteCommandService(repository.GitCliService);
        repository.Commit(repository.ClonePath, "local.txt", "cancelled work");
        var remoteHead = repository.Head(repository.BarePath);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => service.PushAsync(
            repository.ClonePath,
            GitPushMode.Normal,
            remoteName: null,
            cancellation.Token));
        Assert.Equal(remoteHead, repository.Head(repository.BarePath));
    }

    [Fact]
    public async Task AddUpdateAndRemoveAsync_ManageFetchAndPushUrls()
    {
        using var repository = TemporaryRemoteGitRepository.Create();
        var service = new GitRemoteCommandService(repository.GitCliService);
        var pushPath = Path.Combine(Path.GetDirectoryName(repository.BarePath)!, "push.git");

        await service.AddAsync(
            repository.ClonePath,
            "backup",
            repository.BarePath,
            pushPath,
            CancellationToken.None);
        Assert.Equal(repository.BarePath, repository.RemoteUrl("backup"));
        Assert.Equal(pushPath, repository.RemoteUrl("backup", push: true));

        await service.UpdateAsync(
            repository.ClonePath,
            "backup",
            "mirror",
            pushPath,
            pushUrl: null,
            CancellationToken.None);
        Assert.Equal(pushPath, repository.RemoteUrl("mirror"));
        Assert.Equal(pushPath, repository.RemoteUrl("mirror", push: true));
        Assert.DoesNotContain("backup", repository.RemoteNames());

        await service.RemoveAsync(repository.ClonePath, "mirror", CancellationToken.None);
        Assert.DoesNotContain("mirror", repository.RemoteNames());
    }

    [Theory]
    [InlineData(" bad", "https://example.invalid/repository.git")]
    [InlineData("backup", "-unsafe")]
    [InlineData("backup", "https://example.invalid/repository.git\nunsafe")]
    public async Task AddAsync_RejectsUnsafeNameOrUrl(string name, string url)
    {
        var directory = Directory.CreateTempSubdirectory("lovelygit-invalid-remote-");
        var sentinel = Path.Combine(directory.FullName, "sentinel.txt");
        await File.WriteAllTextAsync(sentinel, "unchanged");

        try
        {
            await Assert.ThrowsAsync<ArgumentException>(() =>
                new GitRemoteCommandService(new GitCliService()).AddAsync(
                    directory.FullName,
                    name,
                    url,
                    null,
                    CancellationToken.None));
            Assert.Equal("unchanged", await File.ReadAllTextAsync(sentinel));
            Assert.False(Directory.Exists(Path.Combine(directory.FullName, ".git")));
        }
        finally
        {
            directory.Delete(recursive: true);
        }
    }

}
