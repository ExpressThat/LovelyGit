using ExpressThat.LovelyGit.Services.Git.Cli;
using ExpressThat.LovelyGit.Services.Git.CommitGraph;

namespace LovelyGit.Tests.Git.CommitGraph;

public sealed class CommitGraphManagerCursorTests
{
    [Fact]
    public async Task GetCommitGraphPageAsync_ResumesFromCursorAfterReopen()
    {
        using var temporary = TemporaryGitRepository.Create();
        var repositoryId = Guid.NewGuid();
        var firstOpen = await CommitGraphManager.TryOpenAsync(
            temporary.Path,
            repositoryId,
            null!,
            CancellationToken.None);
        Assert.True(firstOpen.Success);
        using var firstGraph = firstOpen.Graph!;
        var firstPage = await firstGraph.GetCommitGraphPageAsync(
            new CommitGraphCursorState(null, 0),
            1,
            CancellationToken.None);
        var cursorText = CommitGraphManager.EncodeCursorState(firstPage.NextCursor);

        var secondOpen = await CommitGraphManager.TryOpenAsync(
            temporary.Path,
            repositoryId,
            null!,
            CancellationToken.None);
        Assert.True(secondOpen.Success);
        using var secondGraph = secondOpen.Graph!;
        var resumedPage = await secondGraph.GetCommitGraphPageAsync(
            CommitGraphManager.DecodeCursorState(cursorText),
            1,
            CancellationToken.None);

        Assert.Equal(1, resumedPage.Response.Rows.Single().RowIndex);
        Assert.NotEqual(
            firstPage.Response.Rows.Single().Commit.Hash,
            resumedPage.Response.Rows.Single().Commit.Hash);
    }

    [Fact]
    public async Task GetCommitGraphPageAsync_IncludesRemoteRepositoryUrlOncePerPage()
    {
        using var temporary = TemporaryGitRepository.Create();
        temporary.AddRemote("origin", "git@github.com:example/repo.git");
        var open = await CommitGraphManager.TryOpenAsync(
            temporary.Path,
            Guid.NewGuid(),
            null!,
            CancellationToken.None);
        Assert.True(open.Success);
        using var graph = open.Graph!;

        var page = await graph.GetCommitGraphPageAsync(
            new CommitGraphCursorState(null, 0),
            1,
            CancellationToken.None);

        Assert.Equal("https://github.com/example/repo", page.Response.RemoteRepositoryUrl);
        var json = System.Text.Json.JsonSerializer.Serialize(
            page.Response,
            new System.Text.Json.JsonSerializerOptions
            {
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
            });
        Assert.Equal(1, CountOccurrences(json, "RemoteRepositoryUrl"));
        Assert.DoesNotContain("\"RemoteUrl\"", json, StringComparison.Ordinal);
        Assert.DoesNotContain("\"Parents\"", json, StringComparison.Ordinal);
    }

    private static int CountOccurrences(string value, string search)
    {
        var count = 0;
        var offset = 0;
        while ((offset = value.IndexOf(search, offset, StringComparison.Ordinal)) >= 0)
        {
            count++;
            offset += search.Length;
        }

        return count;
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

        public void AddRemote(string name, string url)
        {
            RunGit(new GitCliService(), Path, ["remote", "add", name, url]);
        }

        public static TemporaryGitRepository Create()
        {
            var directory = Directory.CreateTempSubdirectory("lovelygit-graph-cursor-");
            var gitCliService = new GitCliService();

            RunGit(gitCliService, directory.FullName, ["init"]);
            RunGit(gitCliService, directory.FullName, ["config", "user.name", "LovelyGit Test"]);
            RunGit(gitCliService, directory.FullName, ["config", "user.email", "test@example.invalid"]);
            RunGit(gitCliService, directory.FullName, ["commit", "--allow-empty", "-m", "First"]);
            RunGit(gitCliService, directory.FullName, ["commit", "--allow-empty", "-m", "Second"]);
            RunGit(gitCliService, directory.FullName, ["commit", "--allow-empty", "-m", "Third"]);

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
