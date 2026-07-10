using ExpressThat.LovelyGit.Services.Git.Cli;
using ExpressThat.LovelyGit.Services.Git.FileBlame;
using LovelyGit.Tests.Git.Branches;

namespace LovelyGit.Tests.Git.FileBlame;

public sealed class NativeFileBlameReaderTests
{
    [Fact]
    public async Task ReadAsync_AttributesModifiedAndInsertedLinesToTheirCommits()
    {
        using var repository = TemporaryGitRepository.Create();
        var added = await WriteAndCommitAsync(
            repository, "story.txt", "alpha\nbeta\ngamma\n", "2020-01-01T00:00:00Z", "add story");
        var modified = await WriteAndCommitAsync(
            repository, "story.txt", "alpha\nBETA\ngamma\n", "2021-01-01T00:00:00Z", "edit beta");
        var inserted = await WriteAndCommitAsync(
            repository, "story.txt", "alpha\nBETA\nnew line\ngamma\n", "2022-01-01T00:00:00Z", "insert line");

        var response = await ReadAsync(repository, "story.txt");

        Assert.False(response.IsPartial);
        Assert.Equal(4, response.LineCount);
        Assert.Equal(4, response.ResolvedLineCount);
        Assert.Equal(
            [added, modified, inserted, added],
            ExpandHashes(response));
    }

    [Fact]
    public async Task ReadAsync_FollowsAnExactContentRename()
    {
        using var repository = TemporaryGitRepository.Create();
        var added = await WriteAndCommitAsync(
            repository, "old.txt", "one\ntwo\n", "2020-01-01T00:00:00Z", "add old");
        await RunGitAsync(repository, "mv", "old.txt", "new.txt");
        await CommitAsync(repository, "2021-01-01T00:00:00Z", "rename file");

        var response = await ReadAsync(repository, "new.txt");

        Assert.All(response.Hunks, hunk => Assert.Equal(added, hunk.Hash));
        Assert.Equal(2, response.ResolvedLineCount);
    }

    [Fact]
    public async Task ReadAsync_TracesLinesThroughDifferentMergeParents()
    {
        using var repository = TemporaryGitRepository.Create();
        var mainBranch = (await RunGitAsync(repository, "branch", "--show-current")).Trim();
        var added = await WriteAndCommitAsync(
            repository, "merge.txt", "top\nkeep one\nkeep two\nkeep three\nbottom\n", "2020-01-01T00:00:00Z", "add merge file");
        await RunGitAsync(repository, "switch", "-c", "feature-blame");
        var feature = await WriteAndCommitAsync(
            repository, "merge.txt", "top\nkeep one\nkeep two\nkeep three\nFEATURE\n", "2021-01-01T00:00:00Z", "feature line");
        await RunGitAsync(repository, "switch", mainBranch);
        var main = await WriteAndCommitAsync(
            repository, "merge.txt", "MAIN\nkeep one\nkeep two\nkeep three\nbottom\n", "2022-01-01T00:00:00Z", "main line");
        await RunGitAsync(repository, "merge", "--no-ff", "feature-blame", "-m", "merge feature");

        var response = await ReadAsync(repository, "merge.txt");

        Assert.Equal([main, added, added, added, feature], ExpandHashes(response));
    }

    [Fact]
    public async Task ReadAsync_ReportsPartialAttributionWhenTraversalIsBounded()
    {
        using var repository = TemporaryGitRepository.Create();
        await WriteAndCommitAsync(
            repository, "story.txt", "one\ntwo\n", "2020-01-01T00:00:00Z", "add story");
        var latest = await WriteAndCommitAsync(
            repository, "story.txt", "one\ntwo\nthree\n", "2021-01-01T00:00:00Z", "add three");

        var response = await NativeFileBlameReader.ReadAsync(
            repository.Path,
            "story.txt",
            null,
            maximumCommits: 1,
            maximumDuration: Timeout.InfiniteTimeSpan,
            CancellationToken.None);

        Assert.True(response.IsPartial);
        Assert.Equal(1, response.ResolvedLineCount);
        Assert.Equal(latest, response.Hunks[^1].Hash);
        Assert.Null(response.Hunks[0].Hash);
    }

    [Fact]
    public async Task ReadAsync_RejectsBinaryFiles()
    {
        using var repository = TemporaryGitRepository.Create();
        await File.WriteAllBytesAsync(Path.Combine(repository.Path, "binary.dat"), [1, 0, 2]);
        await RunGitAsync(repository, "add", "binary.dat");
        await RunGitAsync(repository, "commit", "-m", "binary");

        var exception = await Assert.ThrowsAsync<InvalidDataException>(
            () => ReadAsync(repository, "binary.dat"));

        Assert.Contains("Binary", exception.Message);
    }

    private static IEnumerable<string?> ExpandHashes(FileBlameResponse response)
    {
        foreach (var hunk in response.Hunks)
        {
            for (var index = 0; index < hunk.LineCount; index++) yield return hunk.Hash;
        }
    }

    private static Task<FileBlameResponse> ReadAsync(
        TemporaryGitRepository repository,
        string path) => NativeFileBlameReader.ReadAsync(
        repository.Path,
        path,
        null,
        maximumCommits: 100,
        maximumDuration: Timeout.InfiniteTimeSpan,
        CancellationToken.None);

    private static async Task<string> WriteAndCommitAsync(
        TemporaryGitRepository repository,
        string path,
        string content,
        string date,
        string subject)
    {
        await File.WriteAllTextAsync(Path.Combine(repository.Path, path), content);
        await RunGitAsync(repository, "add", "--", path);
        await CommitAsync(repository, date, subject);
        return (await RunGitAsync(repository, "rev-parse", "HEAD")).Trim();
    }

    private static Task<string> CommitAsync(
        TemporaryGitRepository repository,
        string date,
        string subject) => RunGitAsync(repository, "commit", $"--date={date}", "-m", subject);

    private static async Task<string> RunGitAsync(
        TemporaryGitRepository repository,
        params string[] arguments)
    {
        var result = await new GitCliService().ExecuteBufferedAsync(
            arguments,
            repository.Path,
            cancellationToken: CancellationToken.None);
        return result.StandardOutput;
    }
}
