using ExpressThat.LovelyGit.Services.Hubs.Commands;
using ExpressThat.LovelyGit.Services.Json;

namespace ExpressThat.LovelyGit.Services.Hubs.CommandResolvers.CommitGraph;

internal static class CommitGraphServiceCollectionExtensions
{
    public static IServiceCollection AddCommitGraphCommands(this IServiceCollection services)
    {
        services.AddLovelyGitJsonTypeInfoResolver(CommitGraphJsonSerializerContext.Default);
        services.AddSingleton<ICommandResponder, CommitGraphCommandResolver>();
        services.AddSingleton<ICommandResponder, GetCommitDetailsCommandResolver>();

        return services;
    }
}
