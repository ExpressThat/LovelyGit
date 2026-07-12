using ExpressThat.LovelyGit.Services.Git.BranchComparison;
using LovelyGit.Tests.Git.Branches;

namespace LovelyGit.Tests.Git.BranchComparison;

public sealed class NativeBranchComparisonReaderTests
{
    [Fact]
    public async Task ReadAsync_ReturnsDivergentCommitsAndDirectTreeChanges()
    {
        using var repository = TemporaryGitRepository.Create();
        await CommitFilesAsync(repository, "Shared base", ("shared.txt", "base"));
        var baseHash = await HeadAsync(repository);
        await RunAsync(repository, "branch", "feature", baseHash);
        await CommitFilesAsync(
            repository, "Current one", ("shared.txt", "current"), ("current.txt", "current"));
        await CommitFilesAsync(repository, "Current two", ("second.txt", "second"));
        await RunAsync(repository, "checkout", "feature");
        await CommitFilesAsync(
            repository, "Target one", ("shared.txt", "target"), ("target.txt", "target"));
        await RunAsync(repository, "checkout", "master");

        var comparison = await NativeBranchComparisonReader.ReadAsync(
            repository.Path, "feature", CancellationToken.None);

        Assert.Equal("master", comparison.CurrentBranchName);
        Assert.Equal("feature", comparison.TargetBranchName);
        Assert.Equal(baseHash, comparison.MergeBaseHash);
        Assert.Equal(2, comparison.AheadCount);
        Assert.Equal(1, comparison.BehindCount);
        Assert.Equal(["Current two", "Current one"], comparison.AheadCommits.Select(commit => commit.Subject));
        Assert.Equal("Target one", Assert.Single(comparison.BehindCommits).Subject);
        Assert.Equal(
            [("current.txt", "Deleted"), ("second.txt", "Deleted"),
                ("shared.txt", "Modified"), ("target.txt", "Added")],
            comparison.Files.Select(file => (file.Path, file.Status)));
        Assert.False(comparison.IsHistoryPartial);
        Assert.False(comparison.IsFileListTruncated);
    }

    [Fact]
    public async Task ReadAsync_RecognizesWhenTargetIsFullyMerged()
    {
        using var repository = TemporaryGitRepository.Create();
        await RunAsync(repository, "checkout", "-b", "feature");
        await CommitFilesAsync(repository, "Feature", ("feature.txt", "feature"));
        await RunAsync(repository, "checkout", "master");
        await CommitFilesAsync(repository, "Current", ("current.txt", "current"));
        await RunAsync(repository, "merge", "--no-ff", "--no-edit", "feature");

        var comparison = await NativeBranchComparisonReader.ReadAsync(
            repository.Path, "feature", CancellationToken.None);

        Assert.Equal(0, comparison.BehindCount);
        Assert.Equal(2, comparison.AheadCount);
        Assert.Empty(comparison.BehindCommits);
    }

    [Fact]
    public async Task ReadAsync_ComparesCurrentBranchWithNativeRemoteRef()
    {
        using var repository = TemporaryGitRepository.Create();
        await CommitFilesAsync(repository, "Shared", ("shared.txt", "base"));
        await RunAsync(repository, "checkout", "-b", "feature");
        await CommitFilesAsync(repository, "Remote feature", ("remote.txt", "remote"));
        var remoteHash = await HeadAsync(repository);
        await RunAsync(repository, "checkout", "master");
        await RunAsync(repository, "update-ref", "refs/remotes/origin/feature", remoteHash);
        await RunAsync(repository, "branch", "-D", "feature");

        var comparison = await NativeBranchComparisonReader.ReadAsync(
            repository.Path, "origin/feature", CancellationToken.None);

        Assert.Equal("origin/feature", comparison.TargetBranchName);
        Assert.Equal(remoteHash, comparison.TargetHash);
        Assert.Equal("Remote feature", Assert.Single(comparison.BehindCommits).Subject);
    }

    [Fact]
    public async Task ReadAsync_RejectsUnknownOrDetachedBranches()
    {
        using var repository = TemporaryGitRepository.Create();
        await Assert.ThrowsAsync<ArgumentException>(() => NativeBranchComparisonReader.ReadAsync(
            repository.Path, "missing", CancellationToken.None));
        await RunAsync(repository, "checkout", "--detach");
        await Assert.ThrowsAsync<InvalidOperationException>(() => NativeBranchComparisonReader.ReadAsync(
            repository.Path, "master", CancellationToken.None));
    }

    [Fact]
    public async Task ReadCommitsAsync_ComparesTwoAuthoritativeCommitTrees()
    {
        using var repository = TemporaryGitRepository.Create();
        await CommitFilesAsync(repository, "Base", ("shared.txt", "base"));
        var baseHash = await HeadAsync(repository);
        await CommitFilesAsync(repository, "Later", ("shared.txt", "later"), ("later.txt", "added"));
        var laterHash = await HeadAsync(repository);

        var comparison = await NativeBranchComparisonReader.ReadCommitsAsync(
            repository.Path, baseHash, laterHash, CancellationToken.None);

        Assert.Equal(baseHash[..7], comparison.CurrentBranchName);
        Assert.Equal(laterHash[..7], comparison.TargetBranchName);
        Assert.Equal(0, comparison.AheadCount);
        Assert.Equal(1, comparison.BehindCount);
        Assert.Equal("Later", Assert.Single(comparison.BehindCommits).Subject);
        Assert.Equal(
            [("later.txt", "Added"), ("shared.txt", "Modified")],
            comparison.Files.Select(file => (file.Path, file.Status)));
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-a-hash")]
    [InlineData("0123456")]
    public async Task ReadCommitsAsync_RejectsInvalidCommitIdentity(string value)
    {
        var directory = Directory.CreateTempSubdirectory("lovelygit-invalid-comparison-");
        try
        {
            await Assert.ThrowsAsync<ArgumentException>(() =>
                NativeBranchComparisonReader.ReadCommitsAsync(
                    directory.FullName,
                    value,
                    new string('1', 40),
                    CancellationToken.None));
            Assert.False(Directory.Exists(Path.Combine(directory.FullName, ".git")));
        }
        finally
        {
            directory.Delete(recursive: true);
        }
    }

    private static async Task CommitFilesAsync(
        TemporaryGitRepository repository,
        string subject,
        params (string Path, string Content)[] files)
    {
        foreach (var file in files)
        {
            await File.WriteAllTextAsync(Path.Combine(repository.Path, file.Path), file.Content);
        }

        await RunAsync(repository, "add", ".");
        await RunAsync(repository, "commit", "-m", subject);
    }

    private static async Task<string> HeadAsync(TemporaryGitRepository repository) =>
        (await RunAsync(repository, "rev-parse", "HEAD")).Trim();

    private static async Task<string> RunAsync(
        TemporaryGitRepository repository,
        params string[] arguments)
    {
        var result = await repository.GitCliService.ExecuteBufferedAsync(
            arguments, repository.Path, cancellationToken: CancellationToken.None);
        return result.StandardOutput;
    }
}
