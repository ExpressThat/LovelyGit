namespace LovelyGit.Tests.Git.RepositoryOperations;

public sealed class TestRepositoryIsolationTests
{
    [Fact]
    public async Task Create_ReturnsIndependentCopiesOfInitializedTemplate()
    {
        using var first = TestRepository.Create();
        using var second = TestRepository.Create();

        await first.CommitFileAsync("first.txt", "first", "first copy change");

        Assert.NotEqual(first.Path, second.Path);
        Assert.True(File.Exists(Path.Combine(first.Path, "first.txt")));
        Assert.False(File.Exists(Path.Combine(second.Path, "first.txt")));
        Assert.Equal("base", await File.ReadAllTextAsync(Path.Combine(second.Path, "shared.txt")));
        Assert.NotEqual(await first.GetHeadHashAsync(), await second.GetHeadHashAsync());
    }

    [Fact]
    public void Create_IsSafeWhenCopiesAreRequestedConcurrently()
    {
        var repositories = Enumerable.Range(0, 8)
            .AsParallel()
            .Select(_ => TestRepository.Create())
            .ToArray();

        try
        {
            Assert.Equal(8, repositories.Select(repository => repository.Path).Distinct().Count());
            Assert.All(repositories, repository =>
                Assert.True(File.Exists(Path.Combine(repository.Path, ".git", "HEAD"))));
        }
        finally
        {
            foreach (var repository in repositories) repository.Dispose();
        }
    }
}
