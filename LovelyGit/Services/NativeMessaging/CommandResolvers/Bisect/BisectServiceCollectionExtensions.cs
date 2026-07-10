using ExpressThat.LovelyGit.Services.Git.Bisect;
using ExpressThat.LovelyGit.Services.Json;
using ExpressThat.LovelyGit.Services.NativeMessaging.Commands;

namespace ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.Bisect;

internal static class BisectServiceCollectionExtensions
{
    public static IServiceCollection AddBisectCommands(this IServiceCollection services)
    {
        services.AddLovelyGitJsonTypeInfoResolver(BisectJsonSerializerContext.Default);
        services.AddSingleton<NativeGitBisectStateReader>();
        services.AddSingleton<GitBisectCommandService>();
        services.AddSingleton<ICommandResponder, GetBisectStateCommandResolver>();
        services.AddSingleton<ICommandResponder, ManageBisectCommandResolver>();
        return services;
    }
}
