using ExpressThat.LovelyGit.Services.NativeMessaging.Commands;
using ExpressThat.LovelyGit.Services.Platform;
using ExpressThat.LovelyGit.Services.Json;

namespace ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.KnownRepository;

internal static class KnownRepositoryServiceCollectionExtensions
{
    public static IServiceCollection AddKnownRepositoryCommands(this IServiceCollection services)
    {
        services.AddLovelyGitJsonTypeInfoResolver(KnownRepositoriesJsonSerializerContext.Default);
        services.AddSingleton<RepositoryRevealService>();
        services.AddSingleton<RepositoryTerminalService>();
        services.AddSingleton<CloneRepositoryProgressPublisher>();
        services.AddSingleton<ICommandResponder, KnownGitRepositorysCommandResolver>();
        services.AddSingleton<ICommandResponder, AddKnownGitRepositorysCommandResolver>();
        services.AddSingleton<ICommandResponder, RemoveKnownGitRepositorysCommandResolver>();
        services.AddSingleton<ICommandResponder, RevealKnownGitRepositoryCommandResolver>();
        services.AddSingleton<ICommandResponder, OpenRepositoryTerminalCommandResolver>();
        services.AddSingleton<ICommandResponder, ChooseCloneDestinationCommandResolver>();
        services.AddSingleton<ICommandResponder, CloneRepositoryCommandResolver>();
        services.AddSingleton<ICommandResponder, CancelCloneRepositoryCommandResolver>();
        services.AddSingleton<ICommandResponder, InitializeRepositoryCommandResolver>();

        return services;
    }
}
