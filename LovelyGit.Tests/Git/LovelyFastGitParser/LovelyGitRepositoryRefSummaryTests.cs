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

    [Fact]
    public async Task LoadRefs_DoesNotPeelLightweightTagFromFullyPeeledFile()
    {
        using var temporary = TemporaryGitRepository.Create();
        await temporary.RunGitAsync(["pack-refs", "--all", "--prune"]);
        var paths = await GitRepositoryDiscovery.ResolveRepositoryPathsAsync(
            temporary.Path,
            CancellationToken.None);
        var objectFormat = await GitRepositoryDiscovery.ReadObjectFormatAsync(
            paths.GitDirectory,
            CancellationToken.None);

        var refs = await GitRefReader.LoadRefsAsync(
            paths.GitDirectory,
            objectFormat,
            GitRefReader.DefaultTagLimit,
            CancellationToken.None);

        Assert.False(refs["refs/tags/v1.0.0"].RequiresPeeling);
        Assert.Null(refs["refs/tags/v1.0.0"].PeeledTarget);
    }

    [Fact]
    public async Task LoadRefs_ReusesSnapshotAndInvalidatesAfterRefChange()
    {
        using var temporary = TemporaryGitRepository.Create();
        var paths = await GitRepositoryDiscovery.ResolveRepositoryPathsAsync(
            temporary.Path,
            CancellationToken.None);
        var objectFormat = await GitRepositoryDiscovery.ReadObjectFormatAsync(
            paths.GitDirectory,
            CancellationToken.None);

        var first = await GitRefReader.LoadRefsAsync(
            paths.GitDirectory,
            objectFormat,
            GitRefReader.DefaultTagLimit,
            CancellationToken.None);
        var second = await GitRefReader.LoadRefsAsync(
            paths.GitDirectory,
            objectFormat,
            GitRefReader.DefaultTagLimit,
            CancellationToken.None);
        await temporary.RunGitAsync(["tag", "new-snapshot-tag"]);
        var changed = await GitRefReader.LoadRefsAsync(
            paths.GitDirectory,
            objectFormat,
            GitRefReader.DefaultTagLimit,
            CancellationToken.None);

        Assert.Same(first, second);
        Assert.NotSame(second, changed);
        Assert.Contains("refs/tags/new-snapshot-tag", changed.Keys);
    }

    [Fact]
    public async Task ReadAsync_HonorsCancellationBeforeReadingLooseRefs()
    {
        using var temporary = TemporaryGitRepository.Create();
        var paths = await GitRepositoryDiscovery.ResolveRepositoryPathsAsync(
            temporary.Path,
            CancellationToken.None);
        var objectFormat = await GitRepositoryDiscovery.ReadObjectFormatAsync(
            paths.GitDirectory,
            CancellationToken.None);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            GitRefSummaryReader.ReadAsync(
                paths.GitDirectory,
                objectFormat,
                GitRefReader.DefaultTagLimit,
                cancellation.Token));
    }

    private sealed class TemporaryGitRepository : IDisposable
    {
        private static readonly RepositoryTemplate<bool> Template = new(
            "lovelygit-ref-summary-template-",
            InitializeTemplate);
        private readonly DirectoryInfo _directory;

        private TemporaryGitRepository(DirectoryInfo directory)
        {
            _directory = directory;
            Path = directory.FullName;
        }

        public string Path { get; }

        public static TemporaryGitRepository Create()
        {
            var (directory, _) = Template.CreateCopy("lovelygit-ref-summary-");
            return new TemporaryGitRepository(directory);
        }

        private static bool InitializeTemplate(DirectoryInfo directory)
        {
            var gitCliService = new GitCliService();

            InitializedRepositoryTemplate.CopyInto(directory);
            File.WriteAllText(System.IO.Path.Combine(directory.FullName, "tracked.txt"), "tracked");
            RunGit(gitCliService, directory.FullName, ["add", "tracked.txt"]);
            RunGit(gitCliService, directory.FullName, ["commit", "-m", "Initial"]);
            RunGit(gitCliService, directory.FullName, ["tag", "v1.0.0"]);
            RunGit(gitCliService, directory.FullName, ["update-ref", "refs/remotes/origin/main", "HEAD"]);
            File.AppendAllText(System.IO.Path.Combine(directory.FullName, "tracked.txt"), "changed");
            RunGit(gitCliService, directory.FullName, ["stash", "push", "-m", "Parser stash"]);

            return true;
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
