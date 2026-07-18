using System.Diagnostics;
using System.Text;
using CliWrap;
using ExpressThat.LovelyGit.Services.Git.BranchComparison;
using LovelyGit.Tests.Git.Branches;
using ExpressThat.LovelyGit.Services.Git.Cli;
using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser;
using Xunit.Abstractions;

namespace LovelyGit.Tests.Git.BranchComparison;

[Collection(PerformanceTestCollection.Name)]
public sealed class NativeBranchComparisonPerformanceTests(ITestOutputHelper output)
{
    private const int CommitsPerSide = 5_000;
    private static readonly RepositoryTemplate<TemplateState> Template = new(
        "lovelygit-branch-comparison-template-",
        InitializeTemplate, prewarmCopies: 2);

    [Fact]
    public async Task NamedBranchComparison_DoesNotScaleWithUnrelatedRefs()
    {
        using var repository = TemporaryGitRepository.Create();
        await repository.GitCliService.ExecuteBufferedAsync(
            ["branch", "feature"], repository.Path);
        SeedUnrelatedRefs(repository.Path, repository.HeadCommitHash, 1_500);
        GC.Collect();
        var allocatedBefore = GC.GetTotalAllocatedBytes(precise: true);
        var startedAt = Stopwatch.GetTimestamp();

        var response = await NativeBranchComparisonReader.ReadAsync(
            repository.Path, "feature", CancellationToken.None);

        var elapsed = Stopwatch.GetElapsedTime(startedAt);
        var allocated = GC.GetTotalAllocatedBytes(true) - allocatedBefore;
        output.WriteLine($"ElapsedMs={elapsed.TotalMilliseconds:F2}; AllocatedBytes={allocated:N0}");
        Assert.Equal(0, response.AheadCount);
        Assert.Equal(0, response.BehindCount);
        Assert.True(elapsed < TimeSpan.FromMilliseconds(100), $"Comparison took {elapsed}.");
        Assert.True(allocated < 750_000, $"Comparison allocated {allocated:N0} bytes.");
    }

    [Fact]
    public async Task DivergentTenThousandCommitHistory_RemainsBounded()
    {
        var (directory, state) = Template.CreateCopy("lovelygit-branch-comparison-");
        try
        {
            using (var repository = await LovelyGitRepository.OpenAsync(
                       directory.FullName, CancellationToken.None))
            {
                Assert.True(GitObjectId.TryParse(
                    state.DeepHistoryHash,
                    repository.ObjectFormat,
                    out var deepHistoryId));
                var target = Assert.Single(
                    repository.GetBranches(), branch => branch.Name == "feature").Target;
                GC.Collect();
                var countAllocatedBefore = GC.GetTotalAllocatedBytes(precise: true);
                var countStartedAt = Stopwatch.GetTimestamp();
                var counts = await NativeBranchComparisonReader.CountHistoryAsync(
                    repository,
                    repository.HeadTarget!.Value,
                    target,
                    CancellationToken.None);
                var countElapsed = Stopwatch.GetElapsedTime(countStartedAt);
                var countAllocated = GC.GetTotalAllocatedBytes(true) - countAllocatedBefore;
                output.WriteLine(
                    $"History only: {countElapsed.TotalMilliseconds:F2} ms; {countAllocated:N0} bytes");
                Assert.Equal(CommitsPerSide, counts.AheadCount);
                Assert.Equal(CommitsPerSide, counts.BehindCount);
                Assert.False(GitObjectStore.IsSharedObjectCached(deepHistoryId));
                Assert.True(countElapsed < TimeSpan.FromMilliseconds(300), $"History took {countElapsed}.");
                Assert.True(countAllocated < 16_000_000, "History traversal exceeded its allocation budget.");
            }

            GC.Collect();
            var allocatedBefore = GC.GetTotalAllocatedBytes(precise: true);
            var startedAt = Stopwatch.GetTimestamp();
            var response = await NativeBranchComparisonReader.ReadAsync(
                directory.FullName,
                "feature",
                CancellationToken.None);
            var elapsed = Stopwatch.GetElapsedTime(startedAt);
            var allocated = GC.GetTotalAllocatedBytes(true) - allocatedBefore;

            output.WriteLine(
                $"{state.CommitCount:N0} divergent commits: {elapsed.TotalMilliseconds:F2} ms; " +
                $"{allocated:N0} bytes");
            Assert.Equal(CommitsPerSide, response.AheadCount);
            Assert.Equal(CommitsPerSide, response.BehindCount);
            Assert.False(response.IsHistoryPartial);
            Assert.True(elapsed < TimeSpan.FromMilliseconds(300), $"Comparison took {elapsed}.");
            Assert.True(allocated < 16_000_000, $"Comparison allocated {allocated:N0} bytes.");
        }
        finally
        {
            DeleteDirectory(directory);
        }
    }

    [Fact]
    public async Task DirectCommitComparison_DoesNotLoadUnrelatedRefs()
    {
        var (directory, state) = Template.CreateCopy("lovelygit-direct-comparison-");
        try
        {
            SeedUnrelatedRefs(directory.FullName, state.MainHash, 1_500);
            GC.Collect();
            var allocatedBefore = GC.GetTotalAllocatedBytes(precise: true);
            var startedAt = Stopwatch.GetTimestamp();

            var response = await NativeBranchComparisonReader.ReadCommitsAsync(
                directory.FullName,
                state.MainHash,
                state.FeatureHash,
                CancellationToken.None);

            var elapsed = Stopwatch.GetElapsedTime(startedAt);
            var allocated = GC.GetTotalAllocatedBytes(true) - allocatedBefore;
            output.WriteLine(
                $"Direct comparison: {elapsed.TotalMilliseconds:F2} ms; {allocated:N0} bytes");
            Assert.Equal(CommitsPerSide, response.AheadCount);
            Assert.Equal(CommitsPerSide, response.BehindCount);
            Assert.True(elapsed < TimeSpan.FromMilliseconds(300), $"Comparison took {elapsed}.");
            Assert.True(allocated < 16_000_000, $"Comparison allocated {allocated:N0} bytes.");
        }
        finally
        {
            DeleteDirectory(directory);
        }
    }

    private static TemplateState InitializeTemplate(DirectoryInfo directory)
    {
        var git = new GitCliService();
        Run(git, directory, ["init", "--initial-branch", "main"]);
        git.CreateCommand(["fast-import", "--quiet"], directory.FullName)
            .WithStandardInputPipe(PipeSource.FromString(BuildFastImport(), Encoding.UTF8))
            .ExecuteAsync().GetAwaiter().GetResult();
        Run(git, directory, ["gc", "--prune=now"]);
        return new TemplateState(
            CommitsPerSide * 2,
            Run(git, directory, ["rev-parse", "feature~2500"]).Trim(),
            Run(git, directory, ["rev-parse", "main"]).Trim(),
            Run(git, directory, ["rev-parse", "feature"]).Trim());
    }

    private static string BuildFastImport()
    {
        var import = new StringBuilder(2_000_000);
        AppendCommit(import, "refs/heads/main", 1, null, "base");
        for (var index = 0; index < CommitsPerSide; index++)
        {
            var mark = index + 2;
            AppendCommit(import, "refs/heads/main", mark, mark - 1, $"main-{index}");
        }

        for (var index = 0; index < CommitsPerSide; index++)
        {
            var mark = CommitsPerSide + index + 2;
            AppendCommit(
                import,
                "refs/heads/feature",
                mark,
                index == 0 ? 1 : mark - 1,
                $"feature-{index}");
        }

        return import.AppendLine("done").ToString().ReplaceLineEndings("\n");
    }

    private static void AppendCommit(
        StringBuilder import,
        string reference,
        int mark,
        int? parent,
        string value)
    {
        import.Append("commit ").AppendLine(reference)
            .Append("mark :").AppendLine(mark.ToString())
            .Append("author LovelyGit Test <test@example.invalid> ")
            .Append(1_700_000_000 + mark).AppendLine(" +0000")
            .Append("committer LovelyGit Test <test@example.invalid> ")
            .Append(1_700_000_000 + mark).AppendLine(" +0000")
            .Append("data ").AppendLine(value.Length.ToString()).AppendLine(value);
        if (parent.HasValue)
        {
            import.Append("from :").AppendLine(parent.Value.ToString());
        }

        import.AppendLine("M 100644 inline history.txt")
            .Append("data ").AppendLine((value.Length + 1).ToString())
            .AppendLine(value).AppendLine();
    }

    private static string Run(
        GitCliService git,
        DirectoryInfo directory,
        IReadOnlyList<string> arguments) =>
        git.ExecuteBufferedAsync(arguments, directory.FullName).GetAwaiter().GetResult().StandardOutput;

    private static void DeleteDirectory(DirectoryInfo directory)
    {
        foreach (var file in directory.EnumerateFiles("*", SearchOption.AllDirectories))
        {
            file.Attributes = FileAttributes.Normal;
        }

        directory.Delete(true);
    }

    private static void SeedUnrelatedRefs(string repositoryPath, string commit, int count)
    {
        var heads = Directory.CreateDirectory(
            Path.Combine(repositoryPath, ".git", "refs", "heads", "perf"));
        for (var index = 0; index < count; index++)
        {
            File.WriteAllText(Path.Combine(heads.FullName, $"branch-{index:D4}"), commit + "\n");
        }
    }

    private sealed record TemplateState(
        int CommitCount,
        string DeepHistoryHash,
        string MainHash,
        string FeatureHash);
}
