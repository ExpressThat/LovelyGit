using ExpressThat.LovelyGit.Services.Git.Rebase;
using ExpressThat.LovelyGit.Services.Json;
using ExpressThat.LovelyGit.Services.NativeMessaging.Commands;

namespace ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.Rebase;

internal static class RebaseServiceCollectionExtensions
{
    public static IServiceCollection AddRebaseCommands(this IServiceCollection services)
    {
        services.AddLovelyGitJsonTypeInfoResolver(RebaseJsonSerializerContext.Default);
        services.AddSingleton<GitRebaseCommandService>();
        services.AddSingleton<ICommandResponder, RebaseCurrentBranchOntoBranchCommandResolver>();
        return services;
    }
}
