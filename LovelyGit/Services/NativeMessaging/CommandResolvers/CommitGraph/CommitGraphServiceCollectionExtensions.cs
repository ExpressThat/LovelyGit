using ExpressThat.LovelyGit.Services.Git.CommitGraph;
using ExpressThat.LovelyGit.Services.Git.CommitSearch;
using ExpressThat.LovelyGit.Services.Git.CommitGraph.Queries;
using ExpressThat.LovelyGit.Services.NativeMessaging.Commands;
using ExpressThat.LovelyGit.Services.Json;

namespace ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.CommitGraph;

internal static class CommitGraphServiceCollectionExtensions
{
    public static IServiceCollection AddCommitGraphCommands(this IServiceCollection services)
    {
        services.AddLovelyGitJsonTypeInfoResolver(CommitGraphJsonSerializerContext.Default);
        services.AddSingleton<CommitDetailsService>();
        services.AddSingleton<CommitFileDiffService>();
        services.AddSingleton<CommitPatchService>();
        services.AddSingleton<RepositoryRefsService>();
        services.AddSingleton<CommitDetailsPreloadService>();
        services.AddSingleton<CommitGraphPageService>();
        services.AddSingleton<CommitSearchService>();
        services.AddSingleton<ICommandResponder, CommitGraphCommandResolver>();
        services.AddSingleton<ICommandResponder, GetCommitDetailsCommandResolver>();
        services.AddSingleton<ICommandResponder, GetCommitFileDiffCommandResolver>();
        services.AddSingleton<ICommandResponder, GetCommitPatchCommandResolver>();
        services.AddSingleton<ICommandResponder, GetRepositoryRefsCommandResolver>();
        services.AddSingleton<ICommandResponder, GetReflogCommandResolver>();
        services.AddSingleton<ICommandResponder, SearchCommitsCommandResolver>();

        return services;
    }
}
