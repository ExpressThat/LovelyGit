using ExpressThat.LovelyGit.Services.Git.Merge;
using ExpressThat.LovelyGit.Services.Json;
using ExpressThat.LovelyGit.Services.NativeMessaging.Commands;

namespace ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.Merge;

internal static class MergeServiceCollectionExtensions
{
    public static IServiceCollection AddMergeCommands(this IServiceCollection services)
    {
        services.AddLovelyGitJsonTypeInfoResolver(MergeJsonSerializerContext.Default);
        services.AddSingleton<GitMergeCommandService>();
        services.AddSingleton<ICommandResponder, MergeBranchIntoCurrentCommandResolver>();
        return services;
    }
}
