using ExpressThat.LovelyGit.Services.Git.WorkingTree;
using ExpressThat.LovelyGit.Services.Hubs.Commands;
using ExpressThat.LovelyGit.Services.Json;

namespace ExpressThat.LovelyGit.Services.Hubs.CommandResolvers.WorkingTree;

internal static class WorkingTreeServiceCollectionExtensions
{
    public static IServiceCollection AddWorkingTreeCommands(this IServiceCollection services)
    {
        services.AddLovelyGitJsonTypeInfoResolver(WorkingTreeJsonSerializerContext.Default);
        services.AddSingleton<WorkingTreeChangeService>();
        services.AddSingleton<WorkingTreeIndexService>();
        services.AddSingleton<WorkingTreeWatcherService>();
        services.AddHostedService<ActiveRepositorySettingsWatcher>();
        services.AddSingleton<ICommandResponder, GetWorkingTreeChangesCommandResolver>();
        services.AddSingleton<ICommandResponder, GetWorkingTreeFileDiffCommandResolver>();
        services.AddSingleton<ICommandResponder, StageWorkingTreeFilesCommandResolver>();
        services.AddSingleton<ICommandResponder, UnstageWorkingTreeFilesCommandResolver>();
        services.AddSingleton<ICommandResponder, StageWorkingTreeLineCommandResolver>();
        services.AddSingleton<ICommandResponder, UnstageWorkingTreeLineCommandResolver>();

        return services;
    }
}
