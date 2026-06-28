using ExpressThat.LovelyGit.Services.Json;
using ExpressThat.LovelyGit.Services.NativeMessaging.Commands;

namespace ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.Remotes;

internal static class RemotesServiceCollectionExtensions
{
    public static IServiceCollection AddRemoteCommands(this IServiceCollection services)
    {
        services.AddLovelyGitJsonTypeInfoResolver(RemotesJsonSerializerContext.Default);
        services.AddSingleton<ICommandResponder, GetRepositoryRemotesCommandResolver>();
        return services;
    }
}
