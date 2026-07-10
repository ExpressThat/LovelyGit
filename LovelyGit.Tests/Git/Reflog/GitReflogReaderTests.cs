using ExpressThat.LovelyGit.Services.Git.Reflog;
using LovelyGit.Tests.Git.Branches;

namespace LovelyGit.Tests.Git.Reflog;

public sealed class GitReflogReaderTests
{
    [Fact]
    public async Task ReadAsync_ReturnsNewestBranchEntriesWithParsedIdentityAndSelectors()
    {
        using var repository = TemporaryGitRepository.Create();
        var branch = await RunGitAsync(repository, "branch", "--show-current");
        var initialHash = repository.HeadCommitHash;
        await RunGitAsync(repository, "commit", "--allow-empty", "-m", "second");
        await RunGitAsync(repository, "reset", "--hard", "HEAD~1");

        var response = await GitReflogReader.ReadAsync(
            repository.Path,
            branch.Trim(),
            limit: 2,
            CancellationToken.None);

        Assert.Equal(branch.Trim(), response.ReferenceName);
        Assert.Equal(2, response.Entries.Count);
        Assert.Equal($"{branch.Trim()}@{{0}}", response.Entries[0].Selector);
        Assert.Equal(initialHash, response.Entries[0].NewHash);
        Assert.Contains("reset: moving to HEAD~1", response.Entries[0].Message);
        Assert.Equal("LovelyGit Test", response.Entries[0].ActorName);
        Assert.Equal("test@example.invalid", response.Entries[0].ActorEmail);
        Assert.True(response.Entries[0].TimestampUnixSeconds > 0);
        Assert.Matches("^[+-][0-9]{4}$", response.Entries[0].Timezone);
    }

    [Fact]
    public async Task ReadAsync_UsesPerWorktreeHeadLogForLinkedWorktree()
    {
        using var repository = TemporaryGitRepository.Create();
        var linkedPath = repository.Path + "-reflog-linked";
        try
        {
            await RunGitAsync(repository, "branch", "feature/reflog-linked");
            await RunGitAsync(
                repository,
                "worktree",
                "add",
                linkedPath,
                "feature/reflog-linked");
            await RunGitAtAsync(linkedPath, "commit", "--allow-empty", "-m", "linked commit");
            var linkedHead = (await RunGitAtAsync(linkedPath, "rev-parse", "HEAD")).Trim();

            var response = await GitReflogReader.ReadAsync(
                linkedPath,
                branchName: null,
                limit: 10,
                CancellationToken.None);

            Assert.Equal("HEAD", response.ReferenceName);
            Assert.Equal("HEAD@{0}", response.Entries[0].Selector);
            Assert.Equal(linkedHead, response.Entries[0].NewHash);
            Assert.Contains("commit: linked commit", response.Entries[0].Message);
        }
        finally
        {
            if (Directory.Exists(linkedPath))
            {
                await RunGitAsync(repository, "worktree", "remove", "--force", linkedPath);
            }
        }
    }

    [Fact]
    public async Task ReadAsync_SkipsMalformedTailAndHonorsLimit()
    {
        using var repository = TemporaryGitRepository.Create();
        await RunGitAsync(repository, "commit", "--allow-empty", "-m", "second");
        var headLog = Path.Combine(repository.Path, ".git", "logs", "HEAD");
        await File.AppendAllTextAsync(headLog, "malformed reflog line\n");

        var response = await GitReflogReader.ReadAsync(
            repository.Path,
            branchName: null,
            limit: 1,
            CancellationToken.None);

        Assert.Single(response.Entries);
        Assert.Equal("HEAD@{0}", response.Entries[0].Selector);
        Assert.Contains("commit: second", response.Entries[0].Message);
    }

    private static async Task<string> RunGitAsync(
        TemporaryGitRepository repository,
        params string[] arguments) =>
        await RunGitAtAsync(repository.Path, arguments);

    private static async Task<string> RunGitAtAsync(
        string path,
        params string[] arguments)
    {
        var result = await new ExpressThat.LovelyGit.Services.Git.Cli.GitCliService()
            .ExecuteBufferedAsync(arguments, path, cancellationToken: CancellationToken.None);
        return result.StandardOutput;
    }
}
