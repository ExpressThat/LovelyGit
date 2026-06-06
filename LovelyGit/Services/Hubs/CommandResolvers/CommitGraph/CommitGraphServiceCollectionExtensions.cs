using ExpressThat.LovelyGit.Services.Git.CommitGraph;
using ExpressThat.LovelyGit.Services.Hubs.Commands;
using ExpressThat.LovelyGit.Services.Json;

namespace ExpressThat.LovelyGit.Services.Hubs.CommandResolvers.CommitGraph;

internal static class CommitGraphServiceCollectionExtensions
{
    public static IServiceCollection AddCommitGraphCommands(this IServiceCollection services)
    {
        services.AddLovelyGitJsonTypeInfoResolver(CommitGraphJsonSerializerContext.Default);
        services.AddSingleton<CommitDetailsService>();
        services.AddSingleton<CommitDetailsPreloadService>();
        services.AddSingleton<ICommandResponder, CommitGraphCommandResolver>();
        services.AddSingleton<ICommandResponder, GetCommitDetailsCommandResolver>();

        return services;
    }
}
