using ExpressThat.LovelyGit.Services.Git.Cli;
using ExpressThat.LovelyGit.Services.Git.CommitSearch;
using LovelyGit.Tests.Git.Branches;

namespace LovelyGit.Tests.Git.CommitSearch;

public sealed class CommitSearchServiceSessionTests
{
    [Fact]
    public async Task SearchAsync_RetainsConsumesAndExpiresPartialSessions()
    {
        using var repository = TemporaryGitRepository.Create();
        for (var index = 0; index < 65; index++)
        {
            await CommitAsync(repository, index == 64 ? "newest session needle" : $"commit {index}");
        }
        using var service = new CommitSearchService(TimeSpan.FromMilliseconds(100));
        var repositoryId = Guid.NewGuid();
        Assert.False(service.ExpirationScheduled);

        var initial = await SearchAsync(service, repositoryId, repository, deep: false);

        Assert.True(initial.IsPartial);
        Assert.Equal(64, initial.ScannedCommitCount);
        Assert.Equal(1, service.RetainedSessionCount);
        Assert.True(service.ExpirationScheduled);

        var deep = await SearchAsync(service, repositoryId, repository, deep: true);

        Assert.False(deep.IsPartial);
        Assert.Equal(66, deep.ScannedCommitCount);
        Assert.Equal(0, service.RetainedSessionCount);
        Assert.False(service.ExpirationScheduled);

        await SearchAsync(service, repositoryId, repository, deep: false);
        await WaitForNoRetainedSessionsAsync(service);
        Assert.Equal(0, service.RetainedSessionCount);
        Assert.False(service.ExpirationScheduled);
    }

    private static Task<CommitSearchResponse> SearchAsync(
        CommitSearchService service,
        Guid repositoryId,
        TemporaryGitRepository repository,
        bool deep) =>
        service.SearchAsync(
            repositoryId,
            repository.Path,
            "newest session needle",
            string.Empty,
            string.Empty,
            null,
            null,
            10,
            deep);

    private static async Task CommitAsync(TemporaryGitRepository repository, string subject)
    {
        await new GitCliService().ExecuteBufferedAsync(
            ["commit", "--allow-empty", "-m", subject],
            repository.Path,
            cancellationToken: CancellationToken.None);
    }

    private static async Task WaitForNoRetainedSessionsAsync(CommitSearchService service)
    {
        var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(2);
        while (service.RetainedSessionCount > 0 && DateTime.UtcNow < deadline)
        {
            await Task.Delay(25);
        }
    }
}
