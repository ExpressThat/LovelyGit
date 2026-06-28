using System.Diagnostics;
using ExpressThat.LovelyGit.Services.Git.Cli;
using ExpressThat.LovelyGit.Services.Git.Conflicts;
using ExpressThat.LovelyGit.Services.Git.OperationState;
using LovelyGit.Tests.Git.WorkingTree;

namespace LovelyGit.Tests.Git.Conflicts;

public sealed class GitConflictServiceTests
{
    [Fact]
    public async Task GetStateAsync_ReturnsConflictedFiles()
    {
        using var repository = await ConflictRepository.CreateAsync();
        var service = new GitConflictService(new GitOperationStateService());

        var state = await service.GetStateAsync(repository.Path, CancellationToken.None);

        Assert.Equal(GitOperationKind.Merge, state.Operation.Kind);
        var file = Assert.Single(state.ConflictedFiles);
        Assert.Equal("conflict.ts", file.Path);
        Assert.Equal(1, file.ConflictCount);
        Assert.Contains("Merge branch", state.CommitMessage);
    }

    [Fact]
    public async Task GetStateAsync_ReturnsResolvedFilesAfterConflictIsStaged()
    {
        using var repository = await ConflictRepository.CreateAsync();
        var commandService = new GitConflictCommandService(
            new GitOperationService(new GitCliService()),
            new GitOperationStateService());
        await commandService.ResolveFileAsync(
            repository.Path,
            "conflict.ts",
            GitConflictAction.UseOurs,
            CancellationToken.None);
        var service = new GitConflictService(new GitOperationStateService());

        var state = await service.GetStateAsync(repository.Path, CancellationToken.None);

        Assert.Empty(state.ConflictedFiles);
        var file = Assert.Single(state.ResolvedFiles);
        Assert.Equal("conflict.ts", file.Path);
        Assert.Equal("Resolved", file.Status);
    }

    [Fact]
    public void ExtractConflictPaths_ReadsGitCommitMessageConflictBlock()
    {
        const string message = """
            Merge branch 'feature'

            # Conflicts:
            #	conflict.ts
            #	src/second.ts
            """;

        var paths = GitConflictService.ExtractConflictPaths(message).ToArray();

        Assert.Equal(["conflict.ts", "src/second.ts"], paths);
    }

    [Fact]
    public async Task GetContentAsync_ReadsOursTheirsAndResult()
    {
        using var repository = await ConflictRepository.CreateAsync();
        var service = new GitConflictFileContentService();

        var content = await service.GetContentAsync(
            repository.Path,
            "conflict.ts",
            CancellationToken.None);

        Assert.Contains(content.OursLines, line => line.Text.Contains("ours"));
        Assert.Contains(content.TheirsLines, line => line.Text.Contains("theirs"));
        Assert.Contains(content.ResultLines, line => line.MarkerKind == "OursStart");
        Assert.Equal(1, content.ConflictCount);
    }

    [Fact]
    public async Task ResolveFileAsync_UseOursStagesResolvedFile()
    {
        using var repository = await ConflictRepository.CreateAsync();
        var service = new GitConflictCommandService(
            new GitOperationService(new GitCliService()),
            new GitOperationStateService());

        await service.ResolveFileAsync(
            repository.Path,
            "conflict.ts",
            GitConflictAction.UseOurs,
            CancellationToken.None);

        var unmerged = await RunGitAsync(repository.Path, false, "ls-files", "-u");
        Assert.Equal(string.Empty, unmerged.Output);
        Assert.Equal("export const value = 'ours';", await File.ReadAllTextAsync(repository.FilePath));
    }

    private sealed class ConflictRepository : IDisposable
    {
        private readonly TemporaryDirectory _directory;

        private ConflictRepository(TemporaryDirectory directory)
        {
            _directory = directory;
            Path = directory.Path;
            FilePath = System.IO.Path.Combine(Path, "conflict.ts");
        }

        public string FilePath { get; }
        public string Path { get; }

        public static async Task<ConflictRepository> CreateAsync()
        {
            var directory = TemporaryDirectory.Create("lovelygit-conflict-");
            await RunGitAsync(directory.Path, true, "init");
            await RunGitAsync(directory.Path, true, "config", "user.email", "test@example.com");
            await RunGitAsync(directory.Path, true, "config", "user.name", "Test User");
            await File.WriteAllTextAsync(
                System.IO.Path.Combine(directory.Path, "conflict.ts"),
                "export const value = 'base';");
            await RunGitAsync(directory.Path, true, "add", ".");
            await RunGitAsync(directory.Path, true, "commit", "-m", "base");
            await RunGitAsync(directory.Path, true, "checkout", "-b", "feature");
            await File.WriteAllTextAsync(
                System.IO.Path.Combine(directory.Path, "conflict.ts"),
                "export const value = 'theirs';");
            await RunGitAsync(directory.Path, true, "commit", "-am", "theirs");
            await RunGitAsync(directory.Path, true, "checkout", "-");
            await File.WriteAllTextAsync(
                System.IO.Path.Combine(directory.Path, "conflict.ts"),
                "export const value = 'ours';");
            await RunGitAsync(directory.Path, true, "commit", "-am", "ours");
            var merge = await RunGitAsync(directory.Path, false, "merge", "feature");
            Assert.NotEqual(0, merge.ExitCode);
            return new ConflictRepository(directory);
        }

        public void Dispose() => _directory.Dispose();
    }

    private static async Task<GitRunResult> RunGitAsync(
        string workingDirectory,
        bool requireSuccess,
        params string[] arguments)
    {
        var startInfo = new ProcessStartInfo("git")
        {
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            UseShellExecute = false,
            WorkingDirectory = workingDirectory,
        };
        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        using var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("Could not start git.");
        var output = await process.StandardOutput.ReadToEndAsync();
        var error = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();
        if (requireSuccess)
        {
            Assert.True(process.ExitCode == 0, error);
        }

        return new GitRunResult(process.ExitCode, output, error);
    }

    private sealed record GitRunResult(int ExitCode, string Output, string Error);
}
