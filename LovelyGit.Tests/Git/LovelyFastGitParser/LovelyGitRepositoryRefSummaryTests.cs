using ExpressThat.LovelyGit.Services.Git.Cli;
using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser;
using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser.Refs;

namespace LovelyGit.Tests.Git.LovelyFastGitParser;

public sealed class LovelyGitRepositoryRefSummaryTests
{
    [Fact]
    public async Task GetRefs_ReturnsBranchesRemotesTagsAndStashes()
    {
        using var temporary = TemporaryGitRepository.Create();
        using var repository = await LovelyGitRepository.OpenAsync(
            temporary.Path,
            CancellationToken.None);

        var refs = repository.GetRefs();

        Assert.Contains(refs, reference =>
            reference is { Kind: GitRefKind.Head, Name: "main" });
        Assert.Contains(refs, reference =>
            reference is { Kind: GitRefKind.Remote, Name: "origin/main" });
        Assert.Contains(refs, reference =>
            reference is { Kind: GitRefKind.Tag, Name: "v1.0.0" });
        Assert.Contains(refs, reference =>
            reference is { Kind: GitRefKind.Stash, Name: "stash" });
    }

    [Fact]
    public async Task ReadAsync_ReturnsPanelRefsWithoutUnboundedTags()
    {
        using var temporary = TemporaryGitRepository.Create();
        await temporary.RunGitAsync(["tag", "v1.0.1"]);
        await temporary.RunGitAsync(["pack-refs", "--all", "--prune"]);
        var paths = await GitRepositoryDiscovery.ResolveRepositoryPathsAsync(
            temporary.Path,
            CancellationToken.None);
        var objectFormat = await GitRepositoryDiscovery.ReadObjectFormatAsync(
            paths.GitDirectory,
            CancellationToken.None);

        var summary = await GitRefSummaryReader.ReadAsync(
            paths.GitDirectory,
            objectFormat,
            maxTags: 1,
            CancellationToken.None);

        Assert.Equal("main", summary.CurrentBranchName);
        Assert.Contains("origin", summary.RemotePrefixes);
        Assert.Contains(summary.Refs, reference =>
            reference is { Kind: GitRefKind.Head, Name: "main" });
        Assert.Contains(summary.Refs, reference =>
            reference is { Kind: GitRefKind.Remote, Name: "origin/main" });
        Assert.Single(summary.Refs, reference => reference.Kind == GitRefKind.Tag);
    }

    [Fact]
    public async Task OpenAsync_PeelsLooseAnnotatedTagToItsCommit()
    {
        using var temporary = TemporaryGitRepository.Create();
        await temporary.RunGitAsync(["tag", "--annotate", "release", "--message", "Release"]);

        using var repository = await LovelyGitRepository.OpenAsync(
            temporary.Path,
            CancellationToken.None);

        var branchTarget = Assert.Single(
            repository.GetBranches(),
            reference => reference is { Kind: GitRefKind.Head, Name: "main" }).Target;
        var tagTarget = Assert.Single(
            repository.GetTags(),
            reference => reference.Name == "release").Target;

        Assert.Equal(branchTarget, tagTarget);
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

        public static TemporaryGitRepository Create()
        {
            var directory = Directory.CreateTempSubdirectory("lovelygit-ref-summary-");
            var gitCliService = new GitCliService();

            RunGit(gitCliService, directory.FullName, ["init", "--initial-branch", "main"]);
            RunGit(gitCliService, directory.FullName, ["config", "user.name", "LovelyGit Test"]);
            RunGit(gitCliService, directory.FullName, ["config", "user.email", "test@example.invalid"]);
            File.WriteAllText(System.IO.Path.Combine(directory.FullName, "tracked.txt"), "tracked");
            RunGit(gitCliService, directory.FullName, ["add", "tracked.txt"]);
            RunGit(gitCliService, directory.FullName, ["commit", "-m", "Initial"]);
            RunGit(gitCliService, directory.FullName, ["tag", "v1.0.0"]);
            RunGit(gitCliService, directory.FullName, ["update-ref", "refs/remotes/origin/main", "HEAD"]);
            File.AppendAllText(System.IO.Path.Combine(directory.FullName, "tracked.txt"), "changed");
            RunGit(gitCliService, directory.FullName, ["stash", "push", "-m", "Parser stash"]);

            return new TemporaryGitRepository(directory);
        }

        public async Task RunGitAsync(IReadOnlyList<string> arguments)
        {
            await new GitCliService().ExecuteBufferedAsync(arguments, Path);
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
            IReadOnlyList<string> arguments) =>
            gitCliService.ExecuteBufferedAsync(arguments, workingDirectory).GetAwaiter().GetResult();
    }
}
