using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser;

namespace LovelyGit.Tests.Git.LovelyFastGitParser;

public sealed class GitCommitGraphReaderTests
{
    [Fact]
    public async Task GraphReader_RemainsLazyUntilAncestryIsRequested()
    {
        using var repository = CommitGraphTestRepository.Create();
        var head = repository.Commit("Head", "head.txt");
        repository.WriteGraph();
        using var native = await LovelyGitRepository.OpenObjectDatabaseAsync(
            repository.Path, CancellationToken.None);

        Assert.False(native.IsCommitGraphInitialized);
        _ = await native.GetCommitAsync(GitObjectId.Parse(head), CancellationToken.None);
        Assert.False(native.IsCommitGraphInitialized);

        _ = await native.GetCommitAncestryHeaderAsync(
            GitObjectId.Parse(head), CancellationToken.None);
        Assert.True(native.IsCommitGraphInitialized);
    }

    [Fact]
    public async Task MonolithicGraph_ReturnsAuthoritativeAncestry()
    {
        using var repository = CommitGraphTestRepository.Create();
        var parent = repository.Commit("Parent", "parent.txt");
        var head = repository.Commit("Head", "head.txt");
        repository.WriteGraph();

        using var native = await LovelyGitRepository.OpenObjectDatabaseAsync(
            repository.Path, CancellationToken.None);
        var header = await native.GetCommitAncestryHeaderAsync(
            GitObjectId.Parse(head), CancellationToken.None);

        Assert.True(native.HasCommitGraph);
        Assert.Equal(GitObjectId.Parse(parent), header.FirstParentHash);
        Assert.Equal(repository.Run("rev-parse", $"{head}^{{tree}}"), header.TreeHash?.Value);
        Assert.Equal(long.Parse(repository.Run("show", "-s", "--format=%ct", head)), header.CommitUnixSeconds);
    }

    [Fact]
    public async Task SplitGraph_ResolvesParentFromBaseLayer()
    {
        using var repository = CommitGraphTestRepository.Create();
        var parent = repository.Commit("Parent", "parent.txt");
        repository.Run("commit-graph", "write", "--reachable", "--split=no-merge");
        var head = repository.Commit("Head", "head.txt");
        repository.Run("commit-graph", "write", "--reachable", "--split=no-merge");
        Assert.True(repository.GraphFiles.Count >= 2);

        using var native = await LovelyGitRepository.OpenObjectDatabaseAsync(
            repository.Path, CancellationToken.None);
        var header = await native.GetCommitAncestryHeaderAsync(
            GitObjectId.Parse(head), CancellationToken.None);

        Assert.True(native.HasCommitGraph);
        Assert.Equal(GitObjectId.Parse(parent), header.FirstParentHash);
    }

    [Fact]
    public async Task OctopusMerge_ReadsEveryExtraParentInGitOrder()
    {
        using var repository = CommitGraphTestRepository.Create();
        var baseHash = repository.Run("rev-parse", "HEAD");
        await GitFastImportFixtureSeeder.SeedOctopusAsync(repository.Path, baseHash);
        var head = repository.Run("rev-parse", "HEAD");
        var expected = repository.Run("show", "-s", "--format=%P", head)
            .Split(' ', StringSplitOptions.RemoveEmptyEntries);
        repository.WriteGraph();

        using var native = await LovelyGitRepository.OpenObjectDatabaseAsync(
            repository.Path, CancellationToken.None);
        var header = await native.GetCommitAncestryHeaderAsync(
            GitObjectId.Parse(head), CancellationToken.None);
        var actual = Enumerable.Range(0, header.ParentHashCount)
            .Select(index => header.GetParentHash(index).Value);

        Assert.True(expected.Length > 2);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public async Task CorruptionAfterOpen_FallsBackToObjectDatabase()
    {
        using var repository = CommitGraphTestRepository.Create();
        var parent = repository.Commit("Parent", "parent.txt");
        var head = repository.Commit("Head", "head.txt");
        repository.WriteGraph();
        using var native = await LovelyGitRepository.OpenObjectDatabaseAsync(
            repository.Path, CancellationToken.None);
        Assert.True(native.HasCommitGraph);
        File.SetAttributes(repository.GraphFiles.Single(), FileAttributes.Normal);
        using (var graph = new FileStream(
                   repository.GraphFiles.Single(), FileMode.Open, FileAccess.Write, FileShare.ReadWrite))
            graph.SetLength(8);

        var header = await native.GetCommitAncestryHeaderAsync(
            GitObjectId.Parse(head), CancellationToken.None);

        Assert.Equal(GitObjectId.Parse(parent), header.FirstParentHash);
    }

    [Fact]
    public async Task CorruptionBeforeOpen_DisablesGraphAndFallsBack()
    {
        using var repository = CommitGraphTestRepository.Create();
        var head = repository.Commit("Head", "head.txt");
        repository.WriteGraph();
        var graphPath = repository.GraphFiles.Single();
        File.SetAttributes(graphPath, FileAttributes.Normal);
        using (var graph = new FileStream(
                   graphPath, FileMode.Open, FileAccess.Write, FileShare.ReadWrite))
            graph.WriteByte(0);

        using var native = await LovelyGitRepository.OpenObjectDatabaseAsync(
            repository.Path, CancellationToken.None);
        var header = await native.GetCommitAncestryHeaderAsync(
            GitObjectId.Parse(head), CancellationToken.None);

        Assert.False(native.HasCommitGraph);
        Assert.NotNull(header.TreeHash);
    }

    [Theory]
    [InlineData("config")]
    [InlineData("shallow")]
    [InlineData("replace")]
    public async Task HistoryOverrides_DisableCommitGraph(string mode)
    {
        using var repository = CommitGraphTestRepository.Create();
        var head = repository.Commit("Head", "head.txt");
        repository.WriteGraph();
        repository.AddHistoryOverride(mode, head);

        using var native = await LovelyGitRepository.OpenObjectDatabaseAsync(
            repository.Path, CancellationToken.None);
        var header = await native.GetCommitAncestryHeaderAsync(
            GitObjectId.Parse(head), CancellationToken.None);

        Assert.False(native.HasCommitGraph);
        Assert.NotNull(header.TreeHash);
    }

    [Fact]
    public async Task PreCancelledGraphRead_ThrowsCancellation()
    {
        using var repository = CommitGraphTestRepository.Create();
        var head = repository.Commit("Head", "head.txt");
        repository.WriteGraph();
        using var native = await LovelyGitRepository.OpenObjectDatabaseAsync(
            repository.Path, CancellationToken.None);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            native.GetCommitAncestryHeaderAsync(GitObjectId.Parse(head), cancellation.Token));
    }

    [Fact]
    public async Task Sha256Graph_UsesRepositoryObjectFormat()
    {
        using var repository = CommitGraphTestRepository.Create(sha256: true);
        var parent = repository.Commit("Parent", "parent.txt");
        var head = repository.Commit("Head", "head.txt");
        repository.WriteGraph();

        using var native = await LovelyGitRepository.OpenObjectDatabaseAsync(
            repository.Path, CancellationToken.None);
        var header = await native.GetCommitAncestryHeaderAsync(
            GitObjectId.Parse(head), CancellationToken.None);

        Assert.True(native.HasCommitGraph);
        Assert.Equal(GitObjectFormat.Sha256, header.FirstParentHash.ObjectFormat);
        Assert.Equal(parent, header.FirstParentHash.Value);
    }

}
