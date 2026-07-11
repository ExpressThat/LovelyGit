using ExpressThat.LovelyGit.Services.Git.Cli;
using ExpressThat.LovelyGit.Services.Git.CommitGraph;
using ExpressThat.LovelyGit.Services.Git.CommitGraph.Models;
using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser;

namespace LovelyGit.Tests.Git.CommitGraph;

public sealed class MergeParentComparisonTests
{
    [Fact]
    public async Task Details_SelectChangedFilesRelativeToRequestedParent()
    {
        using var fixture = MergeFixture.Create();
        using var repository = await LovelyGitRepository.OpenAsync(
            fixture.Path,
            CancellationToken.None);
        var merge = await repository.GetCommitAsync(fixture.MergeId, CancellationToken.None);
        var firstParent = await repository.GetCommitAsync(
            merge.ParentHashes[0],
            CancellationToken.None);
        var secondParent = await repository.GetCommitAsync(
            merge.ParentHashes[1],
            CancellationToken.None);
        var builder = new CommitDetailsBuilder(repository);

        var againstFirst = await builder.BuildAsync(merge, firstParent, CancellationToken.None);
        var againstSecond = await builder.BuildAsync(merge, secondParent, CancellationToken.None);

        Assert.Equal("feature.txt", Assert.Single(againstFirst.ChangedFiles).Path);
        Assert.Equal("main.txt", Assert.Single(againstSecond.ChangedFiles).Path);
        Assert.Equal(merge.ParentHashes.Select(id => id.ToString()), againstSecond.Parents);
    }

    [Fact]
    public async Task FileDiff_SelectsRequestedParentWithoutUsingFirstParentCache()
    {
        using var fixture = MergeFixture.Create();
        using var service = new CommitFileDiffService(null!);

        var againstFirst = await service.GetCommitFileDiffAsync(
            Guid.NewGuid(),
            fixture.Path,
            fixture.MergeId.ToString(),
            0,
            "feature.txt",
            CommitDiffViewMode.Combined,
            ignoreWhitespace: true,
            CancellationToken.None);
        var againstSecond = await service.GetCommitFileDiffAsync(
            Guid.NewGuid(),
            fixture.Path,
            fixture.MergeId.ToString(),
            1,
            "main.txt",
            CommitDiffViewMode.Combined,
            ignoreWhitespace: true,
            CancellationToken.None);

        Assert.Contains(againstFirst.Lines, line => line.Text == "feature");
        Assert.Contains(againstSecond.Lines, line => line.Text == "main");
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(2)]
    public async Task InvalidParentIndex_IsRejectedByDetailsAndDiffReads(int parentIndex)
    {
        using var fixture = MergeFixture.Create();
        var details = new CommitDetailsService(null!);
        using var diffs = new CommitFileDiffService(null!);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            details.GetCommitDetailsAsync(
                Guid.NewGuid(),
                fixture.Path,
                fixture.MergeId,
                parentIndex,
                CancellationToken.None));
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            diffs.GetCommitFileDiffAsync(
                Guid.NewGuid(),
                fixture.Path,
                fixture.MergeId.ToString(),
                parentIndex,
                "main.txt",
                CommitDiffViewMode.Combined,
                ignoreWhitespace: true,
                CancellationToken.None));
    }

    private sealed class MergeFixture : IDisposable
    {
        private readonly DirectoryInfo _directory;
        private readonly GitCliService _git = new();

        private MergeFixture(DirectoryInfo directory, GitObjectId mergeId)
        {
            _directory = directory;
            MergeId = mergeId;
        }

        public GitObjectId MergeId { get; }
        public string Path => _directory.FullName;

        public static MergeFixture Create()
        {
            var directory = Directory.CreateTempSubdirectory("lovelygit-merge-parent-");
            var fixture = new MergeFixture(directory, default);
            fixture.Run(["init", "-b", "main"]);
            fixture.Run(["config", "user.name", "LovelyGit Test"]);
            fixture.Run(["config", "user.email", "test@example.invalid"]);
            fixture.Run(["commit", "--allow-empty", "-m", "Base"]);
            fixture.Run(["switch", "-c", "feature"]);
            File.WriteAllText(System.IO.Path.Combine(fixture.Path, "feature.txt"), "feature\n");
            fixture.Run(["add", "feature.txt"]);
            fixture.Run(["commit", "-m", "Feature"]);
            fixture.Run(["switch", "main"]);
            File.WriteAllText(System.IO.Path.Combine(fixture.Path, "main.txt"), "main\n");
            fixture.Run(["add", "main.txt"]);
            fixture.Run(["commit", "-m", "Main"]);
            fixture.Run(["merge", "--no-ff", "feature", "-m", "Merge feature"]);
            var merge = fixture.Run(["rev-parse", "HEAD"]).StandardOutput.Trim();
            GitObjectId.TryParse(merge, out var mergeId);
            return new MergeFixture(directory, mergeId);
        }

        public void Dispose()
        {
            foreach (var file in _directory.EnumerateFiles("*", SearchOption.AllDirectories))
                file.Attributes = FileAttributes.Normal;
            _directory.Delete(true);
        }

        private CliWrap.Buffered.BufferedCommandResult Run(IReadOnlyList<string> arguments) =>
            _git.ExecuteBufferedAsync(arguments, Path).GetAwaiter().GetResult();
    }
}
