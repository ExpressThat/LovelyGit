using ExpressThat.LovelyGit.Services.Git.Cli;
using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser;

namespace LovelyGit.Tests.Git.LovelyFastGitParser;

public sealed class GitCommitGraphReaderTests
{
    [Fact]
    public async Task GraphReader_RemainsLazyUntilAncestryIsRequested()
    {
        using var repository = TestRepository.Create();
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
        using var repository = TestRepository.Create();
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
        using var repository = TestRepository.Create();
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
        using var repository = TestRepository.Create();
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
        using var repository = TestRepository.Create();
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
        using var repository = TestRepository.Create();
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
        using var repository = TestRepository.Create();
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
        using var repository = TestRepository.Create();
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
        using var repository = TestRepository.Create(sha256: true);
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

    private sealed class TestRepository : IDisposable
    {
        private readonly DirectoryInfo _directory;
        private readonly GitCliService _git = new();
        private TestRepository(DirectoryInfo directory) => _directory = directory;
        public string Path => _directory.FullName;
        public IReadOnlyList<string> GraphFiles => Directory.Exists(GraphDirectory)
            ? Directory.GetFiles(GraphDirectory, "*.graph")
            : File.Exists(SingleGraph) ? [SingleGraph] : [];
        private string GraphDirectory => System.IO.Path.Combine(
            Path, ".git", "objects", "info", "commit-graphs");
        private string SingleGraph => System.IO.Path.Combine(
            Path, ".git", "objects", "info", "commit-graph");

        public static TestRepository Create(bool sha256 = false)
        {
            var repository = new TestRepository(
                Directory.CreateTempSubdirectory("lovelygit-commit-graph-"));
            if (!sha256)
            {
                InitializedRepositoryTemplate.CopyInto(repository._directory, "master");
                return repository;
            }
            repository.Run("init", "--object-format=sha256");
            repository.Run("config", "user.name", "LovelyGit Test");
            repository.Run("config", "user.email", "test@example.invalid");
            repository.Run("config", "core.autocrlf", "false");
            return repository;
        }

        public string Commit(string subject, string file)
        {
            File.WriteAllText(System.IO.Path.Combine(Path, file), subject + "\n");
            Run("add", file);
            Run("commit", "-m", subject);
            return Run("rev-parse", "HEAD");
        }

        public void WriteGraph() => Run("commit-graph", "write", "--reachable");

        public void AddHistoryOverride(string mode, string head)
        {
            if (mode == "config") Run("config", "core.commitGraph", "false");
            else if (mode == "shallow") File.WriteAllText(System.IO.Path.Combine(Path, ".git", "shallow"), head + "\n");
            else
            {
                var directory = Directory.CreateDirectory(
                    System.IO.Path.Combine(Path, ".git", "refs", "replace"));
                File.WriteAllText(System.IO.Path.Combine(directory.FullName, head), head + "\n");
            }
        }

        public string Run(params string[] arguments) => Run((IReadOnlyList<string>)arguments);
        private string Run(IReadOnlyList<string> arguments) => _git
            .ExecuteBufferedAsync(arguments, Path).GetAwaiter().GetResult().StandardOutput.Trim();

        public void Dispose()
        {
            foreach (var file in _directory.EnumerateFiles("*", SearchOption.AllDirectories))
                file.Attributes = FileAttributes.Normal;
            _directory.Delete(recursive: true);
        }
    }
}
