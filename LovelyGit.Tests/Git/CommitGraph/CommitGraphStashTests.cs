using ExpressThat.LovelyGit.Services.Git.Cli;
using ExpressThat.LovelyGit.Services.Git.CommitGraph;
using ExpressThat.LovelyGit.Services.Git.CommitGraph.Models;

namespace LovelyGit.Tests.Git.CommitGraph;

public sealed class CommitGraphStashTests
{
    [Fact]
    public async Task GetCommitGraphPageAsync_ReturnsStashAboveBaseCommit()
    {
        using var temporary = TemporaryGitRepository.Create();
        var open = await CommitGraphManager.TryOpenAsync(
            temporary.Path,
            Guid.NewGuid(),
            null!,
            CancellationToken.None);
        Assert.True(open.Success);
        using var graph = open.Graph!;

        var page = await graph.GetCommitGraphPageAsync(
            new CommitGraphCursorState(null, 0),
            10,
            CancellationToken.None);

        var rows = page.Response.Rows;
        var stashRow = Assert.Single(
            rows,
            row => row.Commit.Refs.Any(reference =>
                reference.Kind == CommitRefKind.Stash &&
                reference.Name == "stash"));
        var baseRow = Assert.Single(rows, row => row.Commit.Message == "Initial");

        Assert.True(stashRow.RowIndex < baseRow.RowIndex);
        Assert.False(stashRow.IsMergeCommit);
        Assert.DoesNotContain(rows, row => row.Commit.Message.StartsWith("index on "));
        Assert.Equal(2, page.Response.LaneCount);
        Assert.Equal(0, baseRow.Lane);
        Assert.NotEqual(baseRow.Lane, stashRow.Lane);
        Assert.Contains(
            baseRow.EdgesAbove,
            edge => edge.FromLane == stashRow.Lane && edge.ToLane == baseRow.Lane);
    }

    [Fact]
    public async Task GetCommitGraphPageAsync_GivesMultipleStashesSeparateLanes()
    {
        using var temporary = TemporaryGitRepository.Create(stashCount: 3);
        var open = await CommitGraphManager.TryOpenAsync(
            temporary.Path,
            Guid.NewGuid(),
            null!,
            CancellationToken.None);
        Assert.True(open.Success);
        using var graph = open.Graph!;

        var page = await graph.GetCommitGraphPageAsync(
            new CommitGraphCursorState(null, 0),
            10,
            CancellationToken.None);
        var stashRows = page.Response.Rows
            .Where(row => row.Commit.Refs.Any(reference => reference.Kind == CommitRefKind.Stash))
            .ToArray();

        Assert.Equal(3, stashRows.Length);
        Assert.DoesNotContain(0, stashRows.Select(row => row.Lane));
        Assert.Equal(3, stashRows.Select(row => row.Lane).Distinct().Count());
    }

    private sealed class TemporaryGitRepository : IDisposable
    {
        private readonly DirectoryInfo _directory;

        private TemporaryGitRepository(DirectoryInfo directory)
        {
            _directory = directory;
            Path = directory.FullName;
        }

        public string Path { get; }

        public static TemporaryGitRepository Create(int stashCount = 1)
        {
            var directory = Directory.CreateTempSubdirectory("lovelygit-stash-graph-");
            var gitCliService = new GitCliService();

            RunGit(gitCliService, directory.FullName, ["init"]);
            RunGit(gitCliService, directory.FullName, ["config", "user.name", "LovelyGit Test"]);
            RunGit(gitCliService, directory.FullName, ["config", "user.email", "test@example.invalid"]);
            File.WriteAllText(System.IO.Path.Combine(directory.FullName, "tracked.txt"), "tracked");
            RunGit(gitCliService, directory.FullName, ["add", "tracked.txt"]);
            RunGit(gitCliService, directory.FullName, ["commit", "-m", "Initial"]);
            for (var index = 1; index <= stashCount; index++)
            {
                File.AppendAllText(System.IO.Path.Combine(directory.FullName, "tracked.txt"), $"changed {index}");
                RunGit(gitCliService, directory.FullName, ["stash", "push", "-m", $"Graph stash {index}"]);
            }

            return new TemporaryGitRepository(directory);
        }

        public void Dispose()
        {
            foreach (var file in _directory.EnumerateFiles("*", SearchOption.AllDirectories))
            {
                file.Attributes = FileAttributes.Normal;
            }

            _directory.Delete(recursive: true);
        }

        private static CliWrap.Buffered.BufferedCommandResult RunGit(
            GitCliService gitCliService,
            string workingDirectory,
            IReadOnlyList<string> arguments)
        {
            return gitCliService
                .ExecuteBufferedAsync(arguments, workingDirectory)
                .GetAwaiter()
                .GetResult();
        }
    }
}
