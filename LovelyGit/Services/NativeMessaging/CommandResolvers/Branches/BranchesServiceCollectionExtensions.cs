using ExpressThat.LovelyGit.Services.NativeMessaging.Commands;
using ExpressThat.LovelyGit.Services.Json;
using ExpressThat.LovelyGit.Services.Git.Branches;

namespace ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.Branches;

internal static class BranchesServiceCollectionExtensions
{
    public static IServiceCollection AddBranchCommands(this IServiceCollection services)
    {
        services.AddLovelyGitJsonTypeInfoResolver(BranchesJsonSerializerContext.Default);
        services.AddSingleton<GitBranchCommandService>();
        services.AddSingleton<ICommandResponder, CreateBranchFromCommitCommandResolver>();
        return services;
    }
}
