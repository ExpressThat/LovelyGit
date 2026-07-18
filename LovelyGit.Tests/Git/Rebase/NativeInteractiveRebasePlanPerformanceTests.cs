using System.Diagnostics;
using System.Text;
using CliWrap;
using ExpressThat.LovelyGit.Services.Git.Cli;
using ExpressThat.LovelyGit.Services.Git.Rebase;
using Xunit.Abstractions;

namespace LovelyGit.Tests.Git.Rebase;

[Collection(PerformanceTestCollection.Name)]
public sealed class NativeInteractiveRebasePlanPerformanceTests(ITestOutputHelper output)
{
    private static readonly RepositoryTemplate<TemplateState> Template = new(
        "lovelygit-rebase-plan-performance-template-",
        InitializeTemplate, prewarmCopies: 2);

    [Fact]
    public async Task ReadAsync_BuildsMaximumPlanWithinDialogBudget()
    {
        var (directory, state) = Template.CreateCopy("lovelygit-rebase-plan-performance-");
        try
        {
            var allocatedBefore = GC.GetTotalAllocatedBytes(precise: true);
            var startedAt = Stopwatch.GetTimestamp();

            var plan = await NativeInteractiveRebasePlanReader.ReadAsync(
                directory.FullName, state.MaximumPlanBase, CancellationToken.None);

            var elapsed = Stopwatch.GetElapsedTime(startedAt);
            var allocated = GC.GetTotalAllocatedBytes(true) - allocatedBefore;
            output.WriteLine($"ElapsedMs={elapsed.TotalMilliseconds:F2}; AllocatedBytes={allocated:N0}");
            Assert.Equal(100, plan.Commits.Count);
            Assert.Equal("commit-002", plan.Commits[0].Subject);
            Assert.Equal("commit-101", plan.Commits[^1].Subject);
            Assert.True(elapsed < TimeSpan.FromMilliseconds(100), $"Plan took {elapsed}.");
            Assert.True(allocated < 1_200_000, $"Plan allocated {allocated:N0} bytes.");
        }
        finally
        {
            foreach (var file in directory.EnumerateFiles("*", SearchOption.AllDirectories))
            {
                file.Attributes = FileAttributes.Normal;
            }
            directory.Delete(recursive: true);
        }
    }

    [Fact]
    public async Task ReadAsync_RejectsPlanAboveMaximumWithoutMovingHead()
    {
        var (directory, state) = Template.CreateCopy("lovelygit-rebase-plan-limit-");
        try
        {
            var headBefore = Run(new GitCliService(), directory, ["rev-parse", "HEAD"]).Trim();

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                NativeInteractiveRebasePlanReader.ReadAsync(
                    directory.FullName, state.AboveMaximumBase, CancellationToken.None));

            Assert.Contains("100 commits", exception.Message);
            Assert.Equal(headBefore, Run(new GitCliService(), directory, ["rev-parse", "HEAD"]).Trim());
        }
        finally
        {
            foreach (var file in directory.EnumerateFiles("*", SearchOption.AllDirectories))
                file.Attributes = FileAttributes.Normal;
            directory.Delete(recursive: true);
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
        var head = Run(git, directory, ["rev-parse", "HEAD"]).Trim();
        var refs = Directory.CreateDirectory(Path.Combine(directory.FullName, ".git", "refs", "heads", "perf"));
        for (var index = 0; index < 1_500; index++)
        {
            File.WriteAllText(Path.Combine(refs.FullName, $"branch-{index:D4}"), head + "\n");
        }
        return new TemplateState(
            Run(git, directory, ["rev-parse", "main~100"]).Trim(),
            Run(git, directory, ["rev-parse", "main~101"]).Trim());
    }

    private static string BuildFastImport()
    {
        var import = new StringBuilder(32_000);
        for (var index = 0; index <= 101; index++)
        {
            var mark = index + 1;
            var subject = $"commit-{index:D3}";
            import.AppendLine("commit refs/heads/main")
                .Append("mark :").AppendLine(mark.ToString())
                .Append("author LovelyGit Test <test@example.invalid> ")
                .Append(1_700_000_000 + mark).AppendLine(" +0000")
                .Append("committer LovelyGit Test <test@example.invalid> ")
                .Append(1_700_000_000 + mark).AppendLine(" +0000")
                .Append("data ").AppendLine(subject.Length.ToString()).AppendLine(subject);
            if (index > 0) import.Append("from :").AppendLine(index.ToString());
            import.AppendLine();
        }
        return import.AppendLine("done").ToString().ReplaceLineEndings("\n");
    }

    private static string Run(
        GitCliService git,
        DirectoryInfo directory,
        IReadOnlyList<string> arguments) =>
        git.ExecuteBufferedAsync(arguments, directory.FullName)
            .GetAwaiter().GetResult().StandardOutput;

    private sealed record TemplateState(string MaximumPlanBase, string AboveMaximumBase);
}
