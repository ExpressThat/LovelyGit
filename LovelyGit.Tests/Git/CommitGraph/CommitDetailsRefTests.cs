using ExpressThat.LovelyGit.Services.Git.Cli;
using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser;

namespace LovelyGit.Tests.Git.CommitGraph;

public sealed class CommitDetailsRefTests
{
    [Fact]
    public async Task LoadRefsForCommit_ReturnsLooseBranchRemoteAndTagLabels()
    {
        using var fixture = RefFixture.Create();
        await fixture.RunAsync(["branch", "feature", fixture.Selected.ToString()]);
        await fixture.RunAsync([
            "update-ref", "refs/remotes/origin/feature", fixture.Selected.ToString()]);
        await fixture.RunAsync(["tag", "light", fixture.Selected.ToString()]);
        await fixture.RunAsync([
            "tag", "--annotate", "release", "--message", "Release", fixture.Selected.ToString()]);
        await fixture.RunAsync([
            "tag", "--annotate", "other", "--message", "Other", fixture.Other.ToString()]);

        var refs = await fixture.ReadSelectedRefsAsync();

        Assert.Contains(refs, reference => reference is { Kind: GitRefKind.Head, Name: "feature" });
        Assert.Contains(refs, reference => reference is { Kind: GitRefKind.Remote, Name: "origin/feature" });
        Assert.Contains(refs, reference => reference is { Kind: GitRefKind.Tag, Name: "light" });
        Assert.Contains(refs, reference => reference is { Kind: GitRefKind.Tag, Name: "release" });
        Assert.DoesNotContain(refs, reference => reference.Name == "other");
    }

    [Fact]
    public async Task LoadRefsForCommit_ReturnsPackedAnnotatedAndLightweightTags()
    {
        using var fixture = RefFixture.Create();
        await fixture.RunAsync(["branch", "packed", fixture.Selected.ToString()]);
        await fixture.RunAsync(["tag", "light", fixture.Selected.ToString()]);
        await fixture.RunAsync([
            "tag", "--annotate", "release", "--message", "Release", fixture.Selected.ToString()]);
        await fixture.RunAsync(["pack-refs", "--all", "--prune"]);

        var refs = await fixture.ReadSelectedRefsAsync();

        Assert.Contains(refs, reference => reference is { Kind: GitRefKind.Head, Name: "packed" });
        Assert.Contains(refs, reference => reference is { Kind: GitRefKind.Tag, Name: "light" });
        Assert.Contains(refs, reference => reference is { Kind: GitRefKind.Tag, Name: "release" });
    }

    [Fact]
    public async Task LoadRefsForCommit_IgnoresPackedRefWithLooseOverride()
    {
        using var fixture = RefFixture.Create();
        await fixture.RunAsync(["branch", "moved", fixture.Selected.ToString()]);
        await fixture.RunAsync(["pack-refs", "--all", "--prune"]);
        await fixture.RunAsync(["update-ref", "refs/heads/moved", fixture.Other.ToString()]);

        var refs = await fixture.ReadSelectedRefsAsync();

        Assert.DoesNotContain(refs, reference => reference.Name == "moved");
    }

    [Fact]
    public async Task LoadRefsForCommit_HonorsCancellation()
    {
        using var fixture = RefFixture.Create();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        using var repository = await LovelyGitRepository.OpenObjectDatabaseAsync(
            fixture.Path,
            CancellationToken.None);
        var commit = await repository.GetCommitAsync(fixture.Selected, CancellationToken.None);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            repository.LoadRefsForCommitAsync(commit, cancellation.Token));
    }

    private sealed class RefFixture : IDisposable
    {
        private static readonly RepositoryTemplate<FixtureState> Template = new(
            "lovelygit-details-ref-template-",
            InitializeTemplate);
        private readonly DirectoryInfo _directory;
        private readonly GitCliService _git = new();

        private RefFixture(DirectoryInfo directory, FixtureState state)
        {
            _directory = directory;
            Selected = state.Selected;
            Other = state.Other;
        }

        public string Path => _directory.FullName;
        public GitObjectId Selected { get; }
        public GitObjectId Other { get; }

        public static RefFixture Create()
        {
            var (directory, state) = Template.CreateCopy("lovelygit-details-ref-");
            return new RefFixture(directory, state);
        }

        public Task<CliWrap.Buffered.BufferedCommandResult> RunAsync(IReadOnlyList<string> args) =>
            _git.ExecuteBufferedAsync(args, Path);

        public async Task<IReadOnlyList<GitCommitRef>> ReadSelectedRefsAsync()
        {
            using var repository = await LovelyGitRepository.OpenObjectDatabaseAsync(
                Path,
                CancellationToken.None);
            var commit = await repository.GetCommitAsync(Selected, CancellationToken.None);
            await repository.LoadRefsForCommitAsync(commit, CancellationToken.None);
            return commit.Refs;
        }

        public void Dispose()
        {
            foreach (var file in _directory.EnumerateFiles("*", SearchOption.AllDirectories))
                file.Attributes = FileAttributes.Normal;
            _directory.Delete(true);
        }

        private static FixtureState InitializeTemplate(DirectoryInfo directory)
        {
            InitializedRepositoryTemplate.CopyInto(directory);
            var git = new GitCliService();
            var other = ReadHead(git, directory);
            File.WriteAllText(System.IO.Path.Combine(directory.FullName, "selected.txt"), "selected\n");
            git.ExecuteBufferedAsync(["add", "selected.txt"], directory.FullName).GetAwaiter().GetResult();
            git.ExecuteBufferedAsync(["commit", "-m", "Selected"], directory.FullName).GetAwaiter().GetResult();
            return new FixtureState(ReadHead(git, directory), other);
        }

        private static GitObjectId ReadHead(GitCliService git, DirectoryInfo directory) =>
            GitObjectId.Parse(
                git.ExecuteBufferedAsync(["rev-parse", "HEAD"], directory.FullName)
                    .GetAwaiter().GetResult().StandardOutput.Trim(),
                GitObjectFormat.Sha1);
    }

    private readonly record struct FixtureState(GitObjectId Selected, GitObjectId Other);
}
