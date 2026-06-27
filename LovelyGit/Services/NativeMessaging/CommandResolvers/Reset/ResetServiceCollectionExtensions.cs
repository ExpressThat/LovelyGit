using ExpressThat.LovelyGit.Services.Git.Reset;
using ExpressThat.LovelyGit.Services.Json;
using ExpressThat.LovelyGit.Services.NativeMessaging.Commands;

namespace ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.Reset;

internal static class ResetServiceCollectionExtensions
{
    public static IServiceCollection AddResetCommands(this IServiceCollection services)
    {
        services.AddLovelyGitJsonTypeInfoResolver(ResetJsonSerializerContext.Default);
        services.AddSingleton<GitResetCommandService>();
        services.AddSingleton<ICommandResponder, ResetCurrentBranchToCommitCommandResolver>();
        return services;
    }
}
