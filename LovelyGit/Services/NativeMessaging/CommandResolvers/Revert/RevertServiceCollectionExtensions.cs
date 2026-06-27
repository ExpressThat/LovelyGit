using ExpressThat.LovelyGit.Services.Git.Revert;
using ExpressThat.LovelyGit.Services.Json;
using ExpressThat.LovelyGit.Services.NativeMessaging.Commands;

namespace ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.Revert;

internal static class RevertServiceCollectionExtensions
{
    public static IServiceCollection AddRevertCommands(this IServiceCollection services)
    {
        services.AddLovelyGitJsonTypeInfoResolver(RevertJsonSerializerContext.Default);
        services.AddSingleton<GitRevertCommandService>();
        services.AddSingleton<ICommandResponder, RevertCommitCommandResolver>();
        return services;
    }
}
