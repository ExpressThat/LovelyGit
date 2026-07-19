using ExpressThat.LovelyGit.Services.Git.CommitSearch;
using LovelyGit.Tests.Git.Branches;

namespace LovelyGit.Tests.Git.CommitSearch;

public sealed partial class NativeCommitSearchReaderTests
{
    private static readonly RepositoryTemplate<string> ScopeTemplate = new(
        "lovelygit-search-scope-template-",
        InitializeScopeTemplate);

    [Fact]
    public async Task SearchAsync_CombinesAuthorAndInclusiveCalendarDateFilters()
    {
        using var repository = TemporaryGitRepository.Create();
        await SeedAsync(
            repository,
            new("2024-01-01T12:00:00Z", "older match", Author: "Alice"),
            new("2024-06-15T12:00:00Z", "other author", Author: "Bob"),
            new("2024-06-30T23:30:00Z", "wanted match", Author: "Alice"),
            new("2024-07-01T00:00:00Z", "too new", Author: "Alice"));

        var response = await NativeCommitSearchReader.SearchAsync(
            repository.Path,
            "match",
            "alice",
            string.Empty,
            new DateTimeOffset(2024, 6, 1, 0, 0, 0, TimeSpan.Zero).ToUnixTimeSeconds(),
            new DateTimeOffset(2024, 7, 1, 0, 0, 0, TimeSpan.Zero).ToUnixTimeSeconds(),
            10, 100, Timeout.InfiniteTimeSpan, CancellationToken.None);

        var result = Assert.Single(response.Results);
        Assert.Equal("wanted match", result.Subject);
        Assert.Equal("alice", response.Author);
        Assert.Equal(1, response.MatchingCommitCount);
    }

    [Fact]
    public async Task SearchAsync_SupportsFilterOnlySearchWithoutEmptyHashMatching()
    {
        using var repository = TemporaryGitRepository.Create();
        await SeedAsync(
            repository,
            new("2024-01-01T00:00:00Z", "alice result", Author: "Alice"),
            new("2024-02-01T00:00:00Z", "bob result", Author: "Bob"));

        var response = await NativeCommitSearchReader.SearchAsync(
            repository.Path, string.Empty, "Alice", string.Empty, null, null, 10, 100,
            Timeout.InfiniteTimeSpan, CancellationToken.None);

        Assert.Equal("alice result", Assert.Single(response.Results).Subject);
    }

    [Theory]
    [InlineData("topic")]
    [InlineData("origin/topic")]
    [InlineData("topic-tag")]
    [InlineData("refs/heads/topic")]
    public async Task SearchAsync_ScopesTraversalToLocalRemoteAndTagRefs(string scope)
    {
        var (directory, head) = ScopeTemplate.CreateCopy("lovelygit-search-scope-");
        using var repository = TemporaryGitRepository.CreateFromCopy(directory, head);

        var response = await NativeCommitSearchReader.SearchAsync(
            repository.Path, "needle", string.Empty, scope, null, null, 10, 100,
            Timeout.InfiniteTimeSpan, CancellationToken.None);

        Assert.Equal("topic needle", Assert.Single(response.Results).Subject);
        Assert.Equal(scope, response.Scope);
    }

    [Fact]
    public async Task SearchAsync_RejectsMissingScopeWithoutFallingBackToAllHistory()
    {
        using var repository = TemporaryGitRepository.Create();

        var error = await Assert.ThrowsAsync<ArgumentException>(() =>
            NativeCommitSearchReader.SearchAsync(
                repository.Path, string.Empty, string.Empty, "missing", null, null,
                10, 100, Timeout.InfiniteTimeSpan, CancellationToken.None));

        Assert.Contains("Git ref not found", error.Message);
    }

    private static string InitializeScopeTemplate(DirectoryInfo directory)
    {
        var baseHash = InitializedRepositoryTemplate.CopyInto(directory, "master");
        var topicHash = Assert.Single(GitFastImportFixtureSeeder.SeedLinearCommitsAsync(
            directory.FullName,
            "refs/heads/topic",
            baseHash,
            [new("2024-01-01T00:00:00Z", "topic needle")]).GetAwaiter().GetResult());
        var mainHash = Assert.Single(GitFastImportFixtureSeeder.SeedLinearCommitsAsync(
            directory.FullName,
            "refs/heads/master",
            baseHash,
            [new("2024-01-02T00:00:00Z", "main needle")]).GetAwaiter().GetResult());
        WriteRef(directory, "refs/tags/topic-tag", topicHash);
        WriteRef(directory, "refs/remotes/origin/topic", topicHash);
        return mainHash;
    }

    private static void WriteRef(DirectoryInfo directory, string name, string hash)
    {
        var path = System.IO.Path.Combine(
            directory.FullName,
            ".git",
            name.Replace('/', System.IO.Path.DirectorySeparatorChar));
        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path)!);
        File.WriteAllText(path, hash + "\n");
    }
}
