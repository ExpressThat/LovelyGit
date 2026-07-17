using System.Diagnostics;
using ExpressThat.LovelyGit.Services.Git.Cli;
using ExpressThat.LovelyGit.Services.Git.CommitGraph;
using ExpressThat.LovelyGit.Services.Git.CommitGraph.Models;
using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser;
using Xunit.Abstractions;

namespace LovelyGit.Tests.Git.CommitGraph;

[Collection(PerformanceTestCollection.Name)]
public sealed class CommitDetailsRefPerformanceTests(ITestOutputHelper output)
{
    private const int UnrelatedRefCount = 1_500;
    private static readonly RepositoryTemplate<GitObjectId> Template = new(
        "lovelygit-details-refs-template-",
        InitializeTemplate);

    [Fact]
    public async Task RefHeavyRepository_HasBoundedColdRead()
    {
        var (directory, commitId) = Template.CreateCopy("lovelygit-details-refs-");
        try
        {
            var service = CreateService();
            var allocatedBefore = GC.GetTotalAllocatedBytes(precise: true);
            var startedAt = Stopwatch.GetTimestamp();
            var details = await service.GetCommitDetailsAsync(
                Guid.NewGuid(),
                directory.FullName,
                commitId,
                CancellationToken.None);
            var elapsed = Stopwatch.GetElapsedTime(startedAt);
            var allocated = GC.GetTotalAllocatedBytes(precise: true) - allocatedBefore;

            output.WriteLine(
                $"{UnrelatedRefCount:N0} unrelated refs: {elapsed.TotalMilliseconds:N2} ms; " +
                $"{allocated:N0} bytes");
            Assert.Equal(["main"], details.Branches);
            Assert.True(elapsed < TimeSpan.FromMilliseconds(180));
        }
        finally
        {
            DeleteDirectory(directory);
        }
    }

    private static CommitDetailsService CreateService() => new(
        (_, _, _) => Task.FromResult<CommitDetailsResponse?>(null),
        (_, _, _, _) => Task.CompletedTask,
        true);

    private static GitObjectId InitializeTemplate(DirectoryInfo directory)
    {
        InitializedRepositoryTemplate.CopyInto(directory);
        File.WriteAllText(Path.Combine(directory.FullName, "selected.txt"), "selected\n");
        Run(directory, ["add", "selected.txt"]);
        Run(directory, ["commit", "-m", "Selected"]);
        var selected = Run(directory, ["rev-parse", "HEAD"]).StandardOutput.Trim();
        var unrelated = Run(directory, ["rev-parse", "HEAD^"]).StandardOutput.Trim();
        var refsRoot = Path.Combine(directory.FullName, ".git", "refs", "heads", "noise");
        Directory.CreateDirectory(refsRoot);
        for (var index = 0; index < UnrelatedRefCount; index++)
        {
            File.WriteAllText(Path.Combine(refsRoot, $"ref-{index:D4}"), unrelated + "\n");
        }

        return GitObjectId.Parse(selected, GitObjectFormat.Sha1);
    }

    private static CliWrap.Buffered.BufferedCommandResult Run(
        DirectoryInfo directory,
        IReadOnlyList<string> arguments) =>
        new GitCliService().ExecuteBufferedAsync(arguments, directory.FullName)
            .GetAwaiter().GetResult();

    private static void DeleteDirectory(DirectoryInfo directory)
    {
        foreach (var file in directory.EnumerateFiles("*", SearchOption.AllDirectories))
        {
            file.Attributes = FileAttributes.Normal;
        }

        directory.Delete(true);
    }
}
