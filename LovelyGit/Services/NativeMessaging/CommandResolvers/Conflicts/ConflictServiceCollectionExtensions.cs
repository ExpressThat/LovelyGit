using ExpressThat.LovelyGit.Services.Git.Conflicts;
using ExpressThat.LovelyGit.Services.Json;
using ExpressThat.LovelyGit.Services.NativeMessaging.Commands;

namespace ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.Conflicts;

internal static class ConflictServiceCollectionExtensions
{
    public static IServiceCollection AddConflictCommands(this IServiceCollection services)
    {
        services.AddLovelyGitJsonTypeInfoResolver(ConflictJsonSerializerContext.Default);
        services.AddSingleton<ConflictRepositoryResolver>();
        services.AddSingleton<GitConflictService>();
        services.AddSingleton<GitConflictFileContentService>();
        services.AddSingleton<GitConflictCommandService>();
        services.AddSingleton<ICommandResponder, GetConflictStateCommandResolver>();
        services.AddSingleton<ICommandResponder, GetConflictFileContentCommandResolver>();
        services.AddSingleton<ICommandResponder, ResolveConflictFileCommandResolver>();
        services.AddSingleton<ICommandResponder, ContinueConflictOperationCommandResolver>();
        services.AddSingleton<ICommandResponder, AbortConflictOperationCommandResolver>();
        return services;
    }
}
