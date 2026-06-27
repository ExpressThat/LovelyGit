using ExpressThat.LovelyGit.Services.Git.CherryPick;
using ExpressThat.LovelyGit.Services.Json;
using ExpressThat.LovelyGit.Services.NativeMessaging.Commands;

namespace ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.CherryPick;

internal static class CherryPickServiceCollectionExtensions
{
    public static IServiceCollection AddCherryPickCommands(this IServiceCollection services)
    {
        services.AddLovelyGitJsonTypeInfoResolver(CherryPickJsonSerializerContext.Default);
        services.AddSingleton<GitCherryPickCommandService>();
        services.AddSingleton<ICommandResponder, CherryPickCommitCommandResolver>();
        return services;
    }
}
