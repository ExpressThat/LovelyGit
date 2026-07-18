using System.Diagnostics;
using System.Text;
using ExpressThat.LovelyGit.Services.Git.Cli;
using ExpressThat.LovelyGit.Services.Git.CommitGraph.Models;
using ExpressThat.LovelyGit.Services.Git.WorkingTree;
using Xunit.Abstractions;

namespace LovelyGit.Tests.Git.WorkingTree;

[Collection(PerformanceTestCollection.Name)]
public sealed class ConflictResolutionReadPerformanceTests(ITestOutputHelper output)
{
    private const int LineCount = 20_000;
    private const int RefCountPerKind = 500;
    private static readonly RepositoryTemplate<bool> Template = new(
        "lovelygit-conflict-read-template-",
        InitializeTemplate, prewarmCopies: 2);

    [Fact]
    public async Task LargeConflict_InitialAndOptionSwitchReadsRemainBounded()
    {
        var (directory, _) = Template.CreateCopy("lovelygit-conflict-read-");
        try
        {
            var service = new ConflictResolutionService(
                new WorkingTreeIndexService(new GitCliService()));
            GC.Collect();
            var initialAllocatedBefore = GC.GetTotalAllocatedBytes(precise: true);
            var startedAt = Stopwatch.GetTimestamp();
            var initial = await service.ReadAsync(
                directory.FullName,
                "shared.txt",
                CommitDiffViewMode.SideBySide,
                ignoreWhitespace: false,
                CancellationToken.None);
            var initialElapsed = Stopwatch.GetElapsedTime(startedAt);
            var initialAllocated = GC.GetTotalAllocatedBytes(true) - initialAllocatedBefore;

            GC.Collect();
            var optionAllocatedBefore = GC.GetTotalAllocatedBytes(precise: true);
            startedAt = Stopwatch.GetTimestamp();
            var whitespace = await service.ReadAsync(
                directory.FullName,
                "shared.txt",
                CommitDiffViewMode.SideBySide,
                ignoreWhitespace: true,
                CancellationToken.None);
            var optionElapsed = Stopwatch.GetElapsedTime(startedAt);
            var optionAllocated = GC.GetTotalAllocatedBytes(true) - optionAllocatedBefore;

            output.WriteLine(
                $"InitialMs={initialElapsed.TotalMilliseconds:F2}; " +
                $"InitialAllocated={initialAllocated:N0}");
            output.WriteLine(
                $"WhitespaceMs={optionElapsed.TotalMilliseconds:F2}; " +
                $"WhitespaceAllocated={optionAllocated:N0}");
            Assert.Single(initial.Hunks);
            Assert.Single(whitespace.Hunks);
            Assert.NotNull(initial.CurrentComparison);
            Assert.NotNull(initial.IncomingComparison);
            Assert.Equal("main", initial.CurrentSource.RefName);
            Assert.Equal("feature/conflict", initial.IncomingSource.RefName);
            Assert.True(
                initialElapsed < TimeSpan.FromMilliseconds(200),
                $"Initial conflict read took {initialElapsed}.");
            Assert.True(
                initialAllocated < 10_000_000,
                $"Initial conflict read allocated {initialAllocated:N0} bytes.");
            Assert.True(
                optionElapsed < TimeSpan.FromMilliseconds(50),
                $"Whitespace switch took {optionElapsed}.");
            Assert.True(
                optionAllocated < 5_000_000,
                $"Whitespace switch allocated {optionAllocated:N0} bytes.");
        }
        finally
        {
            DeleteDirectory(directory);
        }
    }

    [Fact]
    public async Task LargeConflict_ManualSaveDoesNotLoadRepositoryRefs()
    {
        var (directory, _) = Template.CreateCopy("lovelygit-conflict-save-");
        try
        {
            var git = new GitCliService();
            var service = new ConflictResolutionService(new WorkingTreeIndexService(git));
            var opened = await service.ReadAsync(
                directory.FullName,
                "shared.txt",
                CommitDiffViewMode.SideBySide,
                ignoreWhitespace: false,
                CancellationToken.None);
            var resolved = BuildText("resolved");
            GC.Collect();
            var allocatedBefore = GC.GetTotalAllocatedBytes(precise: true);
            var startedAt = Stopwatch.GetTimestamp();

            await service.ResolveAsync(
                directory.FullName,
                "shared.txt",
                opened.WorktreeFingerprint,
                resolved,
                source: null,
                deleteResult: false,
                CancellationToken.None);
            var elapsed = Stopwatch.GetElapsedTime(startedAt);
            var allocated = GC.GetTotalAllocatedBytes(true) - allocatedBefore;

            output.WriteLine($"SaveMs={elapsed.TotalMilliseconds:F2}; SaveAllocated={allocated:N0}");
            var unmerged = await git.ExecuteBufferedAsync(
                ["ls-files", "--unmerged"],
                directory.FullName,
                cancellationToken: CancellationToken.None);
            Assert.Empty(unmerged.StandardOutput);
            Assert.Equal(resolved, await File.ReadAllTextAsync(
                Path.Combine(directory.FullName, "shared.txt")));
            Assert.True(elapsed < TimeSpan.FromMilliseconds(200), $"Save took {elapsed}.");
            Assert.True(allocated < 1_000_000, $"Save allocated {allocated:N0} bytes.");
        }
        finally
        {
            DeleteDirectory(directory);
        }
    }

    private static bool InitializeTemplate(DirectoryInfo directory)
    {
        InitializedRepositoryTemplate.CopyInto(directory);
        var git = new GitCliService();
        File.WriteAllText(Path.Combine(directory.FullName, "shared.txt"), BuildText("base"));
        Run(git, directory, ["add", "shared.txt"]);
        Run(git, directory, ["commit", "-m", "base"]);
        Run(git, directory, ["checkout", "-b", "feature/conflict"]);
        File.WriteAllText(Path.Combine(directory.FullName, "shared.txt"), BuildText("incoming"));
        Run(git, directory, ["add", "shared.txt"]);
        Run(git, directory, ["commit", "-m", "incoming"]);
        Run(git, directory, ["checkout", "main"]);
        File.WriteAllText(Path.Combine(directory.FullName, "shared.txt"), BuildText("current"));
        Run(git, directory, ["add", "shared.txt"]);
        Run(git, directory, ["commit", "-m", "current"]);
        var merge = git.ExecuteBufferedAsync(
                ["merge", "feature/conflict"],
                directory.FullName,
                validateExitCode: false)
            .GetAwaiter().GetResult();
        if (merge.ExitCode == 0)
        {
            throw new InvalidOperationException("Fixture merge did not conflict.");
        }

        var head = Run(git, directory, ["rev-parse", "HEAD"]).Trim();
        var gitDirectory = Path.Combine(directory.FullName, ".git");
        for (var index = 0; index < RefCountPerKind; index++)
        {
            WriteRef(gitDirectory, $"refs/heads/branch-{index}", head);
            WriteRef(gitDirectory, $"refs/remotes/origin/branch-{index}", head);
            WriteRef(gitDirectory, $"refs/tags/tag-{index}", head);
        }
        return true;
    }

    private static string BuildText(string replacement)
    {
        var text = new StringBuilder(400_000);
        for (var index = 0; index < LineCount; index++)
        {
            text.Append(index == LineCount / 2 ? replacement : $"line {index}").Append('\n');
        }
        return text.ToString();
    }

    private static string Run(
        GitCliService git,
        DirectoryInfo directory,
        IReadOnlyList<string> arguments) =>
        git.ExecuteBufferedAsync(arguments, directory.FullName)
            .GetAwaiter().GetResult().StandardOutput;

    private static void WriteRef(string gitDirectory, string name, string hash)
    {
        var path = Path.Combine(gitDirectory, name.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, hash + "\n");
    }

    private static void DeleteDirectory(DirectoryInfo directory)
    {
        foreach (var file in directory.EnumerateFiles("*", SearchOption.AllDirectories))
        {
            file.Attributes = FileAttributes.Normal;
        }
        directory.Delete(true);
    }
}
