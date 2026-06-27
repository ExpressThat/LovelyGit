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
        services.AddSingleton<ICommandResponder, DeleteBranchCommandResolver>();
        services.AddSingleton<ICommandResponder, PullBranchCommandResolver>();
        services.AddSingleton<ICommandResponder, PushBranchCommandResolver>();
        services.AddSingleton<ICommandResponder, RenameBranchCommandResolver>();
        services.AddSingleton<ICommandResponder, SetBranchUpstreamCommandResolver>();
        services.AddSingleton<ICommandResponder, UnsetBranchUpstreamCommandResolver>();
        return services;
    }
}
