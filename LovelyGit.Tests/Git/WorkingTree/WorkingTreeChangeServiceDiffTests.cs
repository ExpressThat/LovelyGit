using DiffPlex.DiffBuilder.Model;
using ExpressThat.LovelyGit.Services.Git.Cli;
using ExpressThat.LovelyGit.Services.Git.CommitGraph.Models;
using ExpressThat.LovelyGit.Services.Git.WorkingTree;
using ExpressThat.LovelyGit.Services.Git.WorkingTree.Models;

namespace LovelyGit.Tests.Git.WorkingTree;

public sealed class WorkingTreeChangeServiceDiffTests
{
    [Fact]
    public async Task GetFileDiffAsync_CanIgnoreWhitespaceOnlyChanges()
    {
        using var repository = TemporaryGitRepository.Create();
        var service = new WorkingTreeChangeService();
        await File.WriteAllTextAsync(
            Path.Combine(repository.Path, "tracked.txt"),
            "first line\nsecond line  \n",
            CancellationToken.None);

        var exactDiff = await service.GetFileDiffAsync(
            repository.Path,
            "tracked.txt",
            WorkingTreeChangeGroup.Unstaged,
            CommitDiffViewMode.Combined,
            ignoreWhitespace: false,
            CancellationToken.None);
        var whitespaceIgnoredDiff = await service.GetFileDiffAsync(
            repository.Path,
            "tracked.txt",
            WorkingTreeChangeGroup.Unstaged,
            CommitDiffViewMode.Combined,
            ignoreWhitespace: true,
            CancellationToken.None);

        Assert.True(exactDiff.HasDifferences);
        Assert.Contains(
            exactDiff.Lines,
            line => line.ChangeType == ChangeType.Inserted.ToString());
        Assert.False(whitespaceIgnoredDiff.HasDifferences);
    }

    [Fact]
    public async Task GetFileDiffAsync_UntrackedFileDoesNotReadIndex()
    {
        using var repository = TemporaryGitRepository.Create();
        var service = new WorkingTreeChangeService();
        await File.WriteAllTextAsync(
            Path.Combine(repository.Path, "new-file.txt"),
            "new content\n",
            CancellationToken.None);
        await File.WriteAllTextAsync(
            Path.Combine(repository.GitDirectory, "index"),
            "not a git index",
            CancellationToken.None);

        var diff = await service.GetFileDiffAsync(
            repository.Path,
            "new-file.txt",
            WorkingTreeChangeGroup.Untracked,
            CommitDiffViewMode.Combined,
            ignoreWhitespace: false,
            CancellationToken.None);

        Assert.Equal("Added", diff.Status);
        Assert.True(diff.HasDifferences);
        Assert.Contains(diff.Lines, line => line.Text == "new content");
    }

    [Fact]
    public async Task GetFileDiffAsync_TypeScriptSyntaxSpansUseTokenOffsets()
    {
        using var repository = TemporaryGitRepository.Create();
        var service = new WorkingTreeChangeService();
        await File.WriteAllTextAsync(
            Path.Combine(repository.Path, "syntax.ts"),
            "describe(\"LovelyGit syntax\", () => {\n});\n",
            CancellationToken.None);

        var diff = await service.GetFileDiffAsync(
            repository.Path,
            "syntax.ts",
            WorkingTreeChangeGroup.Untracked,
            CommitDiffViewMode.Combined,
            ignoreWhitespace: false,
            CancellationToken.None);
        var line = Assert.Single(diff.Lines, line => line.Text.StartsWith("describe", StringComparison.Ordinal));

        Assert.DoesNotContain(
            line.SyntaxSpans,
            span => span.Scope == "String"
                && span.Start <= 0
                && span.Start + span.Length >= "describe".Length);
        Assert.Contains(
            line.SyntaxSpans,
            span => span.Scope == "String"
                && line.Text.Substring(span.Start, span.Length).Contains("LovelyGit syntax", StringComparison.Ordinal));
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

        public string GitDirectory => System.IO.Path.Combine(Path, ".git");

        public static TemporaryGitRepository Create()
        {
            var directory = Directory.CreateTempSubdirectory("lovelygit-diff-");
            var gitCliService = new GitCliService();

            RunGit(gitCliService, directory.FullName, ["init"]);
            RunGit(gitCliService, directory.FullName, ["config", "user.name", "LovelyGit Test"]);
            RunGit(gitCliService, directory.FullName, ["config", "user.email", "test@example.invalid"]);
            File.WriteAllText(
                System.IO.Path.Combine(directory.FullName, "tracked.txt"),
                "first line\nsecond line\n");
            RunGit(gitCliService, directory.FullName, ["add", "tracked.txt"]);
            RunGit(gitCliService, directory.FullName, ["commit", "-m", "Initial"]);

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
