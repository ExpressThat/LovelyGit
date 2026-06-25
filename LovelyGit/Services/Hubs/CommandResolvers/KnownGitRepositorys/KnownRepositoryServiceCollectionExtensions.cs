using ExpressThat.LovelyGit.Services.NativeMessaging.Commands;
using ExpressThat.LovelyGit.Services.Json;

namespace ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.KnownRepository;

internal static class KnownRepositoryServiceCollectionExtensions
{
    public static IServiceCollection AddKnownRepositoryCommands(this IServiceCollection services)
    {
        services.AddLovelyGitJsonTypeInfoResolver(KnownRepositoriesJsonSerializerContext.Default);
        services.AddSingleton<ICommandResponder, KnownGitRepositorysCommandResolver>();
        services.AddSingleton<ICommandResponder, AddKnownGitRepositorysCommandResolver>();
        services.AddSingleton<ICommandResponder, RemoveKnownGitRepositorysCommandResolver>();

        return services;
    }
}
