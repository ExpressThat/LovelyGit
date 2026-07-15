using ExpressThat.LovelyGit.Services.Git.Cli;
using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser;
using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser.Refs;
using LovelyGit.Tests.Git.Branches;

namespace LovelyGit.Tests.Git.LovelyFastGitParser;

public sealed class GitRefTargetNameReaderTests
{
    [Fact]
    public async Task FindBranchNameAsync_PrefersLocalAndHonorsLoosePackedOverrides()
    {
        using var repository = TemporaryGitRepository.Create();
        var head = Parse(repository.HeadCommitHash);
        await RunAsync(repository, "update-ref", "refs/heads/z-feature", head.ToString());
        await RunAsync(repository, "update-ref", "refs/heads/a-feature", head.ToString());
        await RunAsync(repository, "update-ref", "refs/remotes/origin/feature", head.ToString());

        Assert.Equal(
            "a-feature",
            await GitRefTargetNameReader.FindBranchNameAsync(
                Path.Combine(repository.Path, ".git"),
                GitObjectFormat.Sha1,
                head,
                CancellationToken.None));

        await RunAsync(repository, "pack-refs", "--all", "--prune");
        await RunAsync(repository, "commit", "--allow-empty", "-m", "other");
        var other = (await repository.GitCliService.ExecuteBufferedAsync(
            ["rev-parse", "HEAD"],
            repository.Path,
            cancellationToken: CancellationToken.None)).StandardOutput.Trim();
        await RunAsync(repository, "update-ref", "refs/heads/z-feature", other);
        await RunAsync(repository, "update-ref", "refs/heads/a-feature", other);

        Assert.Equal(
            "origin/feature",
            await GitRefTargetNameReader.FindBranchNameAsync(
                Path.Combine(repository.Path, ".git"),
                GitObjectFormat.Sha1,
                head,
                CancellationToken.None));
    }

    [Fact]
    public async Task FindBranchNameAsync_HonorsCancellation()
    {
        using var repository = TemporaryGitRepository.Create();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            GitRefTargetNameReader.FindBranchNameAsync(
                Path.Combine(repository.Path, ".git"),
                GitObjectFormat.Sha1,
                Parse(repository.HeadCommitHash),
                cancellation.Token));
    }

    private static GitObjectId Parse(string value)
    {
        Assert.True(GitObjectId.TryParse(value, GitObjectFormat.Sha1, out var id));
        return id;
    }

    private static async Task RunAsync(
        TemporaryGitRepository repository,
        params string[] arguments) =>
        _ = await repository.GitCliService.ExecuteBufferedAsync(
            arguments,
            repository.Path,
            cancellationToken: CancellationToken.None);
}
