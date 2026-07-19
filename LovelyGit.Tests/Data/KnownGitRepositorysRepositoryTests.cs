using ExpressThat.LovelyGit.Services.Data;
using ExpressThat.LovelyGit.Services.Data.Models;
using ExpressThat.LovelyGit.Services.Data.Repositorys;
using ExpressThat.LovelyGit.Services.Git.Cli;
using ExpressThat.LovelyGit.Services.Git.Worktrees;
using ExpressThat.LovelyGit.Services.Git.RemoteSync;
using ExpressThat.LovelyGit.Services.NativeMessaging;
using ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.WorkingTree;
using ExpressThat.LovelyGit.Services.NativeMessaging.Commands;
using ExpressThat.LovelyGit.Services.Platform;
using LovelyGit.Tests.Git.Branches;

namespace LovelyGit.Tests.Data;

[Collection(PerformanceTestCollection.Name)]
public sealed class KnownGitRepositorysRepositoryTests
{
    [Fact]
    public async Task MissingRepository_ReturnsNullAndACommandFailure()
    {
        var directory = Directory.CreateTempSubdirectory("lovelygit-known-repositories-");
        var previous = Environment.GetEnvironmentVariable(
            LovelyGitDataDirectory.OverrideEnvironmentVariable);

        try
        {
            Environment.SetEnvironmentVariable(
                LovelyGitDataDirectory.OverrideEnvironmentVariable,
                directory.FullName);
            AppDbContext.RegisterBsonKeys();

            using var context = new AppDbContext();
            var repository = new KnownGitRepositorysRepository(
                context,
                new KnownGitRepositoryOrderRepository(context));

            var result = await repository.TryFindByIdAsync(Guid.NewGuid());

            Assert.Null(result);
            var response = await new GetRemoteSyncStatusCommandResolver(repository).Resolve(
                new NativeCommand<GetRemoteSyncStatusCommandArguments>
                {
                    CommandUniqueId = "missing-repository",
                    CommandType = NativeMessageType.GetRemoteSyncStatus,
                    Arguments = new GetRemoteSyncStatusCommandArguments
                    {
                        RepositoryId = Guid.NewGuid(),
                    },
                });
            Assert.False(response.IsSuccess);
            Assert.Equal("Known repository not found.", response.ErrorMessage);
        }
        finally
        {
            Environment.SetEnvironmentVariable(
                LovelyGitDataDirectory.OverrideEnvironmentVariable,
                previous);
            directory.Delete(recursive: true);
        }
    }

    [Fact]
    public async Task RemoveWorktree_ReturnsRegisteredRepositoryOnlyAfterSuccessfulRemoval()
    {
        using var gitRepository = TemporaryGitRepository.Create();
        var linkedPath = gitRepository.Path + "-registered-worktree";
        var dataDirectory = Directory.CreateTempSubdirectory("lovelygit-worktree-registry-");
        var previous = Environment.GetEnvironmentVariable(
            LovelyGitDataDirectory.OverrideEnvironmentVariable);
        var gitCli = gitRepository.GitCliService;
        await gitCli.ExecuteBufferedAsync(
            ["branch", "feature/registered"],
            gitRepository.Path,
            cancellationToken: CancellationToken.None);
        var worktrees = new GitWorktreeCommandService(new GitOperationService(gitCli));
        await worktrees.CreateAsync(
            gitRepository.Path,
            linkedPath,
            "feature/registered",
            CancellationToken.None);

        try
        {
            Environment.SetEnvironmentVariable(
                LovelyGitDataDirectory.OverrideEnvironmentVariable,
                dataDirectory.FullName);
            AppDbContext.RegisterBsonKeys();
            using var context = new AppDbContext();
            var repositories = new KnownGitRepositorysRepository(
                context,
                new KnownGitRepositoryOrderRepository(context));
            var root = await repositories.AddAsync(KnownRepository(gitRepository.Path));
            var linked = await repositories.AddAsync(KnownRepository(linkedPath));
            var resolver = new ManageWorktreeCommandResolver(
                repositories,
                new RepositoryRevealService(),
                new RepositoryTerminalService(),
                worktrees);
            await File.WriteAllTextAsync(Path.Combine(linkedPath, "dirty.txt"), "dirty");

            var failed = await resolver.Resolve(RemoveCommand(root.Id, linkedPath, force: false));

            Assert.False(failed.IsSuccess);
            Assert.True(Directory.Exists(linkedPath));
            Assert.NotNull(await repositories.TryFindByIdAsync(linked.Id));

            var succeeded = Assert.IsType<CommandResponse<KnownGitRepository?>>(await resolver.Resolve(
                RemoveCommand(root.Id, linkedPath, force: true)));

            Assert.True(succeeded.IsSuccess);
            Assert.Equal(linked.Id, succeeded.Result?.Id);
            Assert.False(Directory.Exists(linkedPath));
            Assert.Null(await repositories.TryFindByIdAsync(linked.Id));
        }
        finally
        {
            Environment.SetEnvironmentVariable(
                LovelyGitDataDirectory.OverrideEnvironmentVariable,
                previous);
            if (Directory.Exists(linkedPath))
            {
                await gitCli.ExecuteBufferedAsync(
                    ["worktree", "remove", "--force", "--", linkedPath],
                    gitRepository.Path,
                    validateExitCode: false,
                    cancellationToken: CancellationToken.None);
            }
            dataDirectory.Delete(recursive: true);
        }
    }

    private static KnownGitRepository KnownRepository(string path) =>
        new()
        {
            Id = Guid.NewGuid(),
            Name = Path.GetFileName(path),
            Path = path,
        };

    private static NativeCommand<ManageWorktreeCommandArguments> RemoveCommand(
        Guid repositoryId,
        string linkedPath,
        bool force) =>
        new()
        {
            CommandUniqueId = Guid.NewGuid().ToString(),
            CommandType = NativeMessageType.ManageWorktree,
            Arguments = new ManageWorktreeCommandArguments
            {
                Action = WorktreeMutationAction.Remove,
                Force = force,
                RepositoryId = repositoryId,
                WorktreePath = linkedPath,
            },
        };
}
