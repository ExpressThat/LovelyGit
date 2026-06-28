using ExpressThat.LovelyGit.Services.Git.WorkingTree;
using ExpressThat.LovelyGit.Services.NativeMessaging.Commands;
using ExpressThat.LovelyGit.Services.Json;

namespace ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.WorkingTree;

internal static class WorkingTreeServiceCollectionExtensions
{
    public static IServiceCollection AddWorkingTreeCommands(this IServiceCollection services)
    {
        services.AddLovelyGitJsonTypeInfoResolver(WorkingTreeJsonSerializerContext.Default);
        services.AddSingleton<WorkingTreeChangeService>();
        services.AddSingleton<WorkingTreeStatusListService>();
        services.AddSingleton<WorkingTreeSummaryService>();
        services.AddSingleton<WorkingTreeIndexService>();
        services.AddSingleton<WorkingTreeWatcherService>();
        services.AddHostedService<ActiveRepositorySettingsWatcher>();
        services.AddSingleton<ICommandResponder, GetWorkingTreeChangesCommandResolver>();
        services.AddSingleton<ICommandResponder, GetWorkingTreeChangeSummaryCommandResolver>();
        services.AddSingleton<ICommandResponder, GetWorkingTreeFileDiffCommandResolver>();
        services.AddSingleton<ICommandResponder, RevealWorkingTreeFileCommandResolver>();
        services.AddSingleton<ICommandResponder, StageWorkingTreeFilesCommandResolver>();
        services.AddSingleton<ICommandResponder, UnstageWorkingTreeFilesCommandResolver>();
        services.AddSingleton<ICommandResponder, DiscardWorkingTreeChangesCommandResolver>();
        services.AddSingleton<ICommandResponder, StageWorkingTreeLineCommandResolver>();
        services.AddSingleton<ICommandResponder, UnstageWorkingTreeLineCommandResolver>();
        services.AddSingleton<ICommandResponder, CommitStagedChangesCommandResolver>();
        services.AddSingleton<ICommandResponder, FetchRepositoryCommandResolver>();
        services.AddSingleton<ICommandResponder, PullRepositoryCommandResolver>();
        services.AddSingleton<ICommandResponder, PushRepositoryCommandResolver>();

        return services;
    }
}
