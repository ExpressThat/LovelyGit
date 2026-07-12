using ExpressThat.LovelyGit.Services.Git.Cli;
using ExpressThat.LovelyGit.Services.Git.FileHistory;
using LovelyGit.Tests.Git.Branches;

namespace LovelyGit.Tests.Git.FileHistory;

public sealed class NativeFileHistoryReaderTests
{
    [Fact]
    public async Task ReadAsync_FollowsExactRenameAndClassifiesFileChanges()
    {
        using var repository = TemporaryGitRepository.Create();
        await WriteAndCommitAsync(repository, "old.txt", "one", "2020-01-01T00:00:00Z", "add old");
        await WriteAndCommitAsync(repository, "old.txt", "two", "2021-01-01T00:00:00Z", "modify old");
        await RunGitAsync(repository, "mv", "old.txt", "new.txt");
        await CommitAsync(repository, "2022-01-01T00:00:00Z", "rename file");
        await WriteAndCommitAsync(repository, "new.txt", "three", "2023-01-01T00:00:00Z", "modify new");

        var response = await ReadAsync(repository, "new.txt");

        Assert.False(response.IsPartial);
        Assert.Equal(4, response.MatchingCommitCount);
        Assert.Equal(
            [FileHistoryChangeKind.Modified, FileHistoryChangeKind.Renamed,
                FileHistoryChangeKind.Modified, FileHistoryChangeKind.Added],
            response.Results.Select(result => result.ChangeKind));
        Assert.Equal("old.txt", response.Results[1].PreviousPath);
        Assert.Equal("old.txt", response.Results[2].Path);
    }

    [Fact]
    public async Task ReadAsync_UsesRequestedCommitAsTraversalStart()
    {
        using var repository = TemporaryGitRepository.Create();
        await WriteAndCommitAsync(repository, "tracked.txt", "one", "2020-01-01T00:00:00Z", "add file");
        var start = (await RunGitAsync(repository, "rev-parse", "HEAD")).Trim();
        await WriteAndCommitAsync(repository, "tracked.txt", "two", "2021-01-01T00:00:00Z", "later change");

        var response = await NativeFileHistoryReader.ReadAsync(
            repository.Path,
            "tracked.txt",
            start,
            limit: 10,
            maximumCommits: 100,
            maximumDuration: Timeout.InfiniteTimeSpan,
            CancellationToken.None);

        Assert.Equal("add file", Assert.Single(response.Results).Subject);
    }

    [Fact]
    public async Task ReadAsync_ReportsDeletionAndBoundedPartialTraversal()
    {
        using var repository = TemporaryGitRepository.Create();
        await WriteAndCommitAsync(repository, "gone.txt", "content", "2020-01-01T00:00:00Z", "add file");
        File.Delete(Path.Combine(repository.Path, "gone.txt"));
        await RunGitAsync(repository, "add", "--all");
        await CommitAsync(repository, "2021-01-01T00:00:00Z", "delete file");
		await RunGitAsync(repository, "commit", "--allow-empty", "-m", "later unrelated work");

        var response = await NativeFileHistoryReader.ReadAsync(
            repository.Path,
            "gone.txt",
            null,
            limit: 10,
            maximumCommits: 2,
            maximumDuration: Timeout.InfiniteTimeSpan,
            CancellationToken.None);

        Assert.True(response.IsPartial);
        Assert.Equal(FileHistoryChangeKind.Deleted, Assert.Single(response.Results).ChangeKind);
    }

    [Theory]
    [InlineData("")]
    [InlineData("../outside.txt")]
    [InlineData("folder//file.txt")]
    public async Task ReadAsync_RejectsInvalidRepositoryRelativePaths(string path)
    {
        var directory = Directory.CreateTempSubdirectory("lovelygit-invalid-history-path-");
        try
        {
            await Assert.ThrowsAsync<ArgumentException>(() => NativeFileHistoryReader.ReadAsync(
                directory.FullName,
                path,
                null,
                limit: 100,
                maximumCommits: 100,
                maximumDuration: Timeout.InfiniteTimeSpan,
                CancellationToken.None));
            Assert.False(Directory.Exists(Path.Combine(directory.FullName, ".git")));
        }
        finally
        {
            directory.Delete(recursive: true);
        }
    }

    private static Task<FileHistoryResponse> ReadAsync(
        TemporaryGitRepository repository,
        string path) => NativeFileHistoryReader.ReadAsync(
        repository.Path,
        path,
        null,
        limit: 100,
        maximumCommits: 100,
        maximumDuration: Timeout.InfiniteTimeSpan,
        CancellationToken.None);

    private static async Task WriteAndCommitAsync(
        TemporaryGitRepository repository,
        string path,
        string content,
        string date,
        string subject)
    {
        await File.WriteAllTextAsync(Path.Combine(repository.Path, path), content);
        await RunGitAsync(repository, "add", "--", path);
        await CommitAsync(repository, date, subject);
    }

    private static Task<string> CommitAsync(
        TemporaryGitRepository repository,
        string date,
        string subject) => RunGitAsync(
        repository, "commit", $"--date={date}", "-m", subject);

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
