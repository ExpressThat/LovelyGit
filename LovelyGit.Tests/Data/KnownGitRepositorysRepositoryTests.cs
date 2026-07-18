using ExpressThat.LovelyGit.Services.Data;
using ExpressThat.LovelyGit.Services.Data.Repositorys;
using ExpressThat.LovelyGit.Services.Git.RemoteSync;
using ExpressThat.LovelyGit.Services.NativeMessaging;
using ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.WorkingTree;
using ExpressThat.LovelyGit.Services.NativeMessaging.Commands;

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
}
