using ExpressThat.LovelyGit.Services.Git.CommitGraph;
using ExpressThat.LovelyGit.Services.Git.CommitGraph.Queries;
using ExpressThat.LovelyGit.Services.Hubs.Commands;
using ExpressThat.LovelyGit.Services.Json;

namespace ExpressThat.LovelyGit.Services.Hubs.CommandResolvers.CommitGraph;

internal static class CommitGraphServiceCollectionExtensions
{
    public static IServiceCollection AddCommitGraphCommands(this IServiceCollection services)
    {
        services.AddLovelyGitJsonTypeInfoResolver(CommitGraphJsonSerializerContext.Default);
        services.AddSingleton<CommitDetailsService>();
        services.AddSingleton<CommitFileDiffService>();
        services.AddSingleton<CommitDetailsPreloadService>();
        services.AddSingleton<CommitGraphPageService>();
        services.AddSingleton<ICommandResponder, CommitGraphCommandResolver>();
        services.AddSingleton<ICommandResponder, CancelCommitDiffPreparationCommandResolver>();
        services.AddSingleton<ICommandResponder, GetCommitDetailsCommandResolver>();
        services.AddSingleton<ICommandResponder, GetCommitFileDiffCommandResolver>();

        return services;
    }
}
