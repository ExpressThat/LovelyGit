using System.Text;
using CliWrap;
using ExpressThat.LovelyGit.Services.Git.Cli;
using ExpressThat.LovelyGit.Services.Git.CommitSearch;
using LovelyGit.Tests.Git.Branches;

namespace LovelyGit.Tests.Git.CommitSearch;

public sealed class CommitSearchServiceSessionTests
{
    private const int CommitCount = 66;
    private static readonly RepositoryTemplate<bool> Template = new(
        "lovelygit-search-session-template-",
        InitializeTemplate,
        prewarmCopies: 1);

    [Fact]
    public async Task SearchAsync_RetainsConsumesAndExpiresPartialSessions()
    {
        var (directory, _) = Template.CreateCopy("lovelygit-search-session-");
        try
        {
            using var service = new CommitSearchService(TimeSpan.FromMilliseconds(100));
            var repositoryId = Guid.NewGuid();
            Assert.False(service.ExpirationScheduled);

            var initial = await SearchAsync(service, repositoryId, directory.FullName, deep: false);

            Assert.True(initial.IsPartial);
            Assert.Equal(64, initial.ScannedCommitCount);
            Assert.Equal(1, service.RetainedSessionCount);
            Assert.True(service.ExpirationScheduled);

            var deep = await SearchAsync(service, repositoryId, directory.FullName, deep: true);

            Assert.False(deep.IsPartial);
            Assert.Equal(CommitCount, deep.ScannedCommitCount);
            Assert.Equal(0, service.RetainedSessionCount);
            Assert.False(service.ExpirationScheduled);

            await SearchAsync(service, repositoryId, directory.FullName, deep: false);
            await WaitForNoRetainedSessionsAsync(service);
            Assert.Equal(0, service.RetainedSessionCount);
            Assert.False(service.ExpirationScheduled);
        }
        finally
        {
            TemporaryGitDirectory.Delete(directory);
        }
    }

    private static Task<CommitSearchResponse> SearchAsync(
        CommitSearchService service,
        Guid repositoryId,
        string repositoryPath,
        bool deep) =>
        service.SearchAsync(
            repositoryId,
            repositoryPath,
            "newest session needle",
            string.Empty,
            string.Empty,
            null,
            null,
            10,
            deep);

    private static bool InitializeTemplate(DirectoryInfo directory)
    {
        var git = new GitCliService();
        git.ExecuteBufferedAsync(
                ["init", "--initial-branch", "master"],
                directory.FullName)
            .GetAwaiter()
            .GetResult();
        git.CreateCommand(["fast-import", "--quiet"], directory.FullName)
            .WithStandardInputPipe(PipeSource.FromString(BuildFastImport(), Encoding.UTF8))
            .ExecuteAsync()
            .GetAwaiter()
            .GetResult();
        return true;
    }

    private static string BuildFastImport()
    {
        var import = new StringBuilder(CommitCount * 180);
        for (var index = 0; index < CommitCount; index++)
        {
            var subject = index == CommitCount - 1
                ? "newest session needle"
                : $"commit {index}";
            import.AppendLine("commit refs/heads/master")
                .Append("mark :").AppendLine((index + 1).ToString())
                .Append("author LovelyGit Test <test@example.invalid> ")
                .Append(1_700_000_000 + index).AppendLine(" +0000")
                .Append("committer LovelyGit Test <test@example.invalid> ")
                .Append(1_700_000_000 + index).AppendLine(" +0000")
                .Append("data ").AppendLine(Encoding.UTF8.GetByteCount(subject).ToString())
                .AppendLine(subject);
            if (index > 0) import.Append("from :").AppendLine(index.ToString());
            import.AppendLine();
        }
        return import.AppendLine("done").ToString().ReplaceLineEndings("\n");
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
