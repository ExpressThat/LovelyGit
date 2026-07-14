using ExpressThat.LovelyGit.Services.Git.CommitGraph;
using ExpressThat.LovelyGit.Services.Git.CommitSearch;
using ExpressThat.LovelyGit.Services.Git.CommitGraph.Queries;
using ExpressThat.LovelyGit.Services.Git.FileHistory;
using ExpressThat.LovelyGit.Services.Git.FileBlame;
using ExpressThat.LovelyGit.Services.NativeMessaging.Commands;
using ExpressThat.LovelyGit.Services.Json;
using ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.BranchComparison;

namespace ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.CommitGraph;

internal static class CommitGraphServiceCollectionExtensions
{
    public static IServiceCollection AddCommitGraphCommands(this IServiceCollection services)
    {
        services.AddLovelyGitJsonTypeInfoResolver(CommitGraphJsonSerialization.Resolver);
        services.AddLovelyGitJsonTypeInfoResolver(BranchComparisonJsonSerializerContext.Default);
        services.AddSingleton<CommitDetailsService>();
        services.AddSingleton<CommitFileDiffService>();
        services.AddSingleton<CommitPatchService>();
        services.AddSingleton<CommitPatchExportService>();
        services.AddSingleton<CommitPatchSeriesService>();
        services.AddSingleton<CommitPatchSeriesExportService>();
        services.AddSingleton<CommitArchiveExportService>();
        services.AddSingleton<RepositoryRefsService>();
        services.AddSingleton<CommitDetailsPreloadService>();
        services.AddSingleton<CommitGraphPageService>();
        services.AddSingleton<CommitSearchService>();
        services.AddSingleton<FileHistoryService>();
        services.AddSingleton<FileBlameService>();
        services.AddSingleton<ICommandResponder, CommitGraphCommandResolver>();
        services.AddSingleton<ICommandResponder, GetCommitDetailsCommandResolver>();
        services.AddSingleton<ICommandResponder, GetCommitFileDiffCommandResolver>();
        services.AddSingleton<ICommandResponder, GetCommitPatchCommandResolver>();
        services.AddSingleton<ICommandResponder, SaveCommitPatchCommandResolver>();
        services.AddSingleton<ICommandResponder, GetCommitPatchSeriesCommandResolver>();
        services.AddSingleton<ICommandResponder, SaveCommitPatchSeriesCommandResolver>();
        services.AddSingleton<ICommandResponder, SaveCommitArchiveCommandResolver>();
        services.AddSingleton<ICommandResponder, GetRepositoryRefsCommandResolver>();
        services.AddSingleton<ICommandResponder, GetBranchComparisonCommandResolver>();
        services.AddSingleton<ICommandResponder, GetReflogCommandResolver>();
        services.AddSingleton<ICommandResponder, SearchCommitsCommandResolver>();
        services.AddSingleton<ICommandResponder, GetFileHistoryCommandResolver>();
        services.AddSingleton<ICommandResponder, GetFileBlameCommandResolver>();

        return services;
    }
}
