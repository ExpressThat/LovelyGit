using ExpressThat.LovelyGit.Services.Git.RemoteSync;
using LovelyGit.Tests.Git.Branches;
using LovelyGit.Tests.Git.Cli;

namespace LovelyGit.Tests.Git.RemoteSync;

public sealed class NativeRemoteSyncStatusReaderTests
{
    [Fact]
    public async Task ReadAsync_TracksAheadBehindAndDivergedStatesFromNativeRefs()
    {
        using var repository = TemporaryRemoteGitRepository.Create();

        var equal = await ReadAsync(repository.ClonePath);
        Assert.Equal("origin/master", equal.UpstreamName);
        Assert.True(equal.HasUpstream);
        Assert.True(equal.IsUpstreamAvailable);
        Assert.Equal(0, equal.AheadCount);
        Assert.Equal(0, equal.BehindCount);

        repository.Commit(repository.ClonePath, "ahead.txt", "local ahead");
        var ahead = await ReadAsync(repository.ClonePath);
        Assert.Equal(1, ahead.AheadCount);
        Assert.Equal(0, ahead.BehindCount);

        repository.RunGit(repository.ClonePath, ["reset", "--hard", "origin/master"]);
        repository.CommitAndPushFromUpdater("behind.txt", "remote ahead");
        repository.RunGit(repository.ClonePath, ["fetch", "origin"]);
        var behind = await ReadAsync(repository.ClonePath);
        Assert.Equal(0, behind.AheadCount);
        Assert.Equal(1, behind.BehindCount);

        repository.Commit(repository.ClonePath, "diverged.txt", "local diverged");
        var diverged = await ReadAsync(repository.ClonePath);
        Assert.Equal(1, diverged.AheadCount);
        Assert.Equal(1, diverged.BehindCount);
        Assert.False(diverged.IsHistoryPartial);
        Assert.NotEqual(diverged.LocalHash, diverged.UpstreamHash);
    }

    [Fact]
    public async Task ReadAsync_ReturnsCalmStatesForNoMissingOrDetachedUpstream()
    {
        using var repository = TemporaryGitRepository.Create();

        var noUpstream = await ReadAsync(repository.Path);
        Assert.Equal("master", noUpstream.BranchName);
        Assert.False(noUpstream.HasUpstream);

        await RunAsync(repository, "config", "branch.master.remote", "origin");
        await RunAsync(repository, "config", "branch.master.merge", "refs/heads/main");
        var missing = await ReadAsync(repository.Path);
        Assert.True(missing.HasUpstream);
        Assert.False(missing.IsUpstreamAvailable);
        Assert.Equal("origin/main", missing.UpstreamName);

        await RunAsync(repository, "checkout", "--detach");
        var detached = await ReadAsync(repository.Path);
        Assert.Null(detached.BranchName);
        Assert.False(detached.HasUpstream);
        Assert.NotNull(detached.LocalHash);
    }

    private static Task<RemoteSyncStatusResponse> ReadAsync(string path) =>
        NativeRemoteSyncStatusReader.ReadAsync(path, CancellationToken.None);

    private static async Task RunAsync(TemporaryGitRepository repository, params string[] arguments)
    {
        await repository.GitCliService.ExecuteBufferedAsync(
            arguments,
            repository.Path,
            cancellationToken: CancellationToken.None);
    }
}
