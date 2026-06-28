using ExpressThat.LovelyGit.Services.Git.Stashes;
using ExpressThat.LovelyGit.Services.Json;
using ExpressThat.LovelyGit.Services.NativeMessaging.Commands;

namespace ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.Stashes;

internal static class StashesServiceCollectionExtensions
{
    public static IServiceCollection AddStashCommands(this IServiceCollection services)
    {
        services.AddLovelyGitJsonTypeInfoResolver(StashesJsonSerializerContext.Default);
        services.AddSingleton<GitStashCommandService>();
        services.AddSingleton<ICommandResponder, StashChangesCommandResolver>();
        return services;
    }
}
