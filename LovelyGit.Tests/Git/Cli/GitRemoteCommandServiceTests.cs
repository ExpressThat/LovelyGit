using ExpressThat.LovelyGit.Services.Git.Cli;
using ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.WorkingTree;

namespace LovelyGit.Tests.Git.Cli;

public sealed class GitRemoteCommandServiceTests
{
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
    public async Task AddUpdateAndRemoveAsync_ManageFetchAndPushUrls()
    {
        using var repository = TemporaryRemoteGitRepository.Create();
        var service = new GitRemoteCommandService(repository.GitCliService);
        var pushPath = Path.Combine(Path.GetDirectoryName(repository.BarePath)!, "push.git");
        repository.RunGit(repository.ClonePath, ["init", "--bare", pushPath]);

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
        using var repository = TemporaryRemoteGitRepository.Create();
        var service = new GitRemoteCommandService(repository.GitCliService);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.AddAsync(repository.ClonePath, name, url, null, CancellationToken.None));
    }

    private sealed class TemporaryRemoteGitRepository : IDisposable
    {
        private readonly DirectoryInfo _directory;

        private TemporaryRemoteGitRepository(DirectoryInfo directory)
        {
            _directory = directory;
            GitCliService = new GitCliService();
            BarePath = Path.Combine(directory.FullName, "origin.git");
            ClonePath = Path.Combine(directory.FullName, "clone");
            UpdaterPath = Path.Combine(directory.FullName, "updater");
        }

        public string BarePath { get; }

        public string ClonePath { get; }

        public GitCliService GitCliService { get; }

        private string UpdaterPath { get; }

        public string[] RemoteNames() =>
            RunGit(ClonePath, ["remote"])
                .StandardOutput
                .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);

        public string RemoteUrl(string name, bool push = false) =>
            RunGit(ClonePath, push
                    ? ["remote", "get-url", "--push", name]
                    : ["remote", "get-url", name])
                .StandardOutput.Trim();

        public void Commit(string path, string fileName, string message)
        {
            File.WriteAllText(Path.Combine(path, fileName), message);
            RunGit(path, ["add", "."]);
            RunGit(path, ["commit", "-m", message]);
        }

        public void CommitAndPushFromUpdater(string fileName, string message)
        {
            Commit(UpdaterPath, fileName, message);
            RunGit(UpdaterPath, ["push", "origin", "HEAD"]);
        }

        public static TemporaryRemoteGitRepository Create()
        {
            var directory = Directory.CreateTempSubdirectory("lovelygit-remote-");
            var repository = new TemporaryRemoteGitRepository(directory);

            repository.RunGit(directory.FullName, ["init", "--bare", repository.BarePath]);
            repository.RunGit(directory.FullName, ["init", repository.UpdaterPath]);
            repository.ConfigureIdentity(repository.UpdaterPath);
            repository.Commit(repository.UpdaterPath, "readme.txt", "initial");
            var branch = repository.RunGit(repository.UpdaterPath, ["branch", "--show-current"])
                .StandardOutput.Trim();
            repository.RunGit(
                repository.UpdaterPath,
                ["remote", "add", "origin", repository.BarePath]);
            repository.RunGit(repository.UpdaterPath, ["push", "-u", "origin", branch]);
            repository.RunGit(
                directory.FullName,
                ["clone", repository.BarePath, repository.ClonePath]);
            repository.ConfigureIdentity(repository.ClonePath);

            return repository;
        }

        public void Dispose()
        {
            foreach (var file in _directory.EnumerateFiles("*", SearchOption.AllDirectories))
            {
                file.Attributes = FileAttributes.Normal;
            }

            _directory.Delete(recursive: true);
        }

        public CliWrap.Buffered.BufferedCommandResult RunGit(
            string workingDirectory,
            IReadOnlyList<string> arguments)
        {
            return GitCliService
                .ExecuteBufferedAsync(arguments, workingDirectory)
                .GetAwaiter()
                .GetResult();
        }

        private void ConfigureIdentity(string path)
        {
            RunGit(path, ["config", "user.name", "LovelyGit Test"]);
            RunGit(path, ["config", "user.email", "test@example.invalid"]);
        }
    }
}
