using ExpressThat.LovelyGit.Services.Hubs.Commands;
using ExpressThat.LovelyGit.Services.Json;

namespace ExpressThat.LovelyGit.Services.Hubs.CommandResolvers.KnownRepository;

internal static class KnownRepositoryServiceCollectionExtensions
{
    public static IServiceCollection AddKnownRepositoryCommands(this IServiceCollection services)
    {
        services.AddLovelyGitJsonTypeInfoResolver(KnownRepositoriesJsonSerializerContext.Default);
        services.AddSingleton<ICommandResponder, KnownGitRepositorysCommandResolver>();
        services.AddSingleton<ICommandResponder, AddKnownGitRepositorysCommandResolver>();

        return services;
    }
}
