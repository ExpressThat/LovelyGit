using ExpressThat.LovelyGit.Services.Git.Cli;
using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser.Operations;
using ExpressThat.LovelyGit.Services.Git.Rebase;
using ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.Rebase;
using LovelyGit.Tests.Git.Branches;

namespace LovelyGit.Tests.Git.Rebase;

[Collection(GitShellIntegrationCollection.Name)]
public sealed class GitInteractiveRebaseServiceTests
{
    [Fact]
    public async Task StartAsync_ReordersRewordsAndDropsCommits()
    {
        using var repository = TemporaryGitRepository.Create();
        await CommitFileAsync(repository, "a.txt", "A", "Commit A");
        await CommitFileAsync(repository, "b.txt", "B", "Commit B");
        await CommitFileAsync(repository, "c.txt", "C", "Commit C");
        var branchName = await CurrentBranchAsync(repository);
        var current = await ReadPlanAsync(repository);
        var plan = new[]
        {
            Item(current.Commits[2], InteractiveRebaseAction.Pick),
            Item(current.Commits[0], InteractiveRebaseAction.Reword, "Commit A rewritten"),
            Item(current.Commits[1], InteractiveRebaseAction.Drop),
        };

        var outcome = await CreateService(repository).StartAsync(
            repository.Path, repository.HeadCommitHash, plan, CancellationToken.None);

        Assert.True(outcome.IsCompleted);
        Assert.Equal(branchName, await CurrentBranchAsync(repository));
        AssertTemporaryPlanRemoved(repository);
        Assert.Equal(
            ["Commit A rewritten", "Commit C", "Initial"],
            await SubjectsAsync(repository));
        Assert.False(File.Exists(Path.Combine(repository.Path, "b.txt")));
    }

    [Fact]
    public async Task StartAsync_SquashesAndFixesUpCommits()
    {
        using var repository = TemporaryGitRepository.Create();
        await CommitFileAsync(repository, "a.txt", "A", "Commit A");
        await CommitFileAsync(repository, "b.txt", "B", "Commit B");
        await CommitFileAsync(repository, "c.txt", "C", "Commit C");
        var branchName = await CurrentBranchAsync(repository);
        var current = await ReadPlanAsync(repository);
        var plan = new[]
        {
            Item(current.Commits[0], InteractiveRebaseAction.Pick),
            Item(current.Commits[1], InteractiveRebaseAction.Squash),
            Item(current.Commits[2], InteractiveRebaseAction.Fixup),
        };

        var outcome = await CreateService(repository).StartAsync(
            repository.Path, repository.HeadCommitHash, plan, CancellationToken.None);

        Assert.True(outcome.IsCompleted);
        Assert.Equal(branchName, await CurrentBranchAsync(repository));
        AssertTemporaryPlanRemoved(repository);
        var subjects = await SubjectsAsync(repository);
        Assert.Equal(2, subjects.Count);
        Assert.Equal("Commit A", subjects[0]);
        Assert.All(["a.txt", "b.txt", "c.txt"], path =>
            Assert.True(File.Exists(Path.Combine(repository.Path, path))));
    }

    [Fact]
    public async Task StartAsync_ConflictPausesAndAbortRestoresExactRepositoryForRetry()
    {
        using var repository = TemporaryGitRepository.Create();
        await CommitFileAsync(repository, "conflict.txt", "base\n", "Conflict base");
        var baseHash = (await RunAsync(repository, "rev-parse", "HEAD")).Trim();
        await CommitFileAsync(repository, "conflict.txt", "current\n", "Current candidate");
        await CommitFileAsync(repository, "conflict.txt", "incoming\n", "Incoming candidate");
        var originalHead = (await RunAsync(repository, "rev-parse", "HEAD")).Trim();
        var originalStatus = await RunAsync(repository, "status", "--porcelain");
        var branchName = await CurrentBranchAsync(repository);
        var current = await NativeInteractiveRebasePlanReader.ReadAsync(
            repository.Path, baseHash, CancellationToken.None);
        var plan = new[]
        {
            Item(current.Commits[1], InteractiveRebaseAction.Pick),
            Item(current.Commits[0], InteractiveRebaseAction.Pick),
        };
        var operations = new GitOperationService(repository.GitCliService);
        var service = new GitInteractiveRebaseService(operations);
        var repositoryOperations = new GitRepositoryOperationService(operations);

        try
        {
            var paused = await service.StartAsync(
                repository.Path, baseHash, plan, CancellationToken.None);

            Assert.False(paused.IsCompleted);
            Assert.Equal(GitRepositoryOperationKind.Rebase, paused.Operation);
            Assert.Contains("resolve and stage", paused.Message, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("<<<<<<<", await File.ReadAllTextAsync(
                Path.Combine(repository.Path, "conflict.txt")));
            AssertTemporaryPlanRetained(repository);

            await repositoryOperations.AbortAsync(
                repository.Path, GitRepositoryOperationKind.Rebase, CancellationToken.None);
            await AssertRepositoryRestoredAsync(
                repository, originalHead, branchName, originalStatus);

            var retry = await service.StartAsync(
                repository.Path, baseHash, plan, CancellationToken.None);
            Assert.Equal(GitRepositoryOperationKind.Rebase, retry.Operation);
        }
        finally
        {
            if (await repositoryOperations.GetOperationAsync(
                repository.Path, CancellationToken.None) == GitRepositoryOperationKind.Rebase)
            {
                await repositoryOperations.AbortAsync(
                    repository.Path, GitRepositoryOperationKind.Rebase, CancellationToken.None);
            }
        }

        await AssertRepositoryRestoredAsync(repository, originalHead, branchName, originalStatus);
    }

    private static GitInteractiveRebaseService CreateService(TemporaryGitRepository repository) =>
        new(new GitOperationService(repository.GitCliService));

    private static InteractiveRebasePlanItem Item(
        InteractiveRebaseCommit commit,
        InteractiveRebaseAction action,
        string? message = null) => new() { Hash = commit.Hash, Action = action, Message = message };

    private static Task<InteractiveRebasePlanResponse> ReadPlanAsync(TemporaryGitRepository repository) =>
        NativeInteractiveRebasePlanReader.ReadAsync(
            repository.Path, repository.HeadCommitHash, CancellationToken.None);

    private static async Task CommitFileAsync(
        TemporaryGitRepository repository,
        string path,
        string content,
        string subject)
    {
        await File.WriteAllTextAsync(Path.Combine(repository.Path, path), content);
        await RunAsync(repository, "add", "--", path);
        await RunAsync(repository, "commit", "-m", subject);
    }

    private static async Task<IReadOnlyList<string>> SubjectsAsync(TemporaryGitRepository repository)
    {
        var output = await RunAsync(repository, "log", "--format=%s");
        return output.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }

    private static async Task<string> CurrentBranchAsync(TemporaryGitRepository repository) =>
        (await RunAsync(repository, "branch", "--show-current")).Trim();

    private static void AssertTemporaryPlanRemoved(TemporaryGitRepository repository) =>
        Assert.False(Directory.Exists(Path.Combine(repository.Path, ".git", "lovelygit", "rebase")));

    private static void AssertTemporaryPlanRetained(TemporaryGitRepository repository) =>
        Assert.True(Directory.Exists(Path.Combine(repository.Path, ".git", "lovelygit", "rebase")));

    private static async Task AssertRepositoryRestoredAsync(
        TemporaryGitRepository repository,
        string expectedHead,
        string expectedBranch,
        string expectedStatus)
    {
        Assert.Equal(expectedHead, (await RunAsync(repository, "rev-parse", "HEAD")).Trim());
        Assert.Equal(expectedBranch, await CurrentBranchAsync(repository));
        Assert.Equal(expectedStatus, await RunAsync(repository, "status", "--porcelain"));
        Assert.Equal("incoming\n", await File.ReadAllTextAsync(
            Path.Combine(repository.Path, "conflict.txt")));
        AssertTemporaryPlanRemoved(repository);
    }

    private static async Task<string> RunAsync(TemporaryGitRepository repository, params string[] arguments)
    {
        var result = await repository.GitCliService.ExecuteBufferedAsync(
            arguments, repository.Path, cancellationToken: CancellationToken.None);
        return result.StandardOutput;
    }
}
