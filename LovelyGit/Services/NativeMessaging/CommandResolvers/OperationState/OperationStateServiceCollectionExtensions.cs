using ExpressThat.LovelyGit.Services.Git.OperationState;
using ExpressThat.LovelyGit.Services.Json;
using ExpressThat.LovelyGit.Services.NativeMessaging.Commands;

namespace ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.OperationState;

internal static class OperationStateServiceCollectionExtensions
{
    public static IServiceCollection AddOperationStateCommands(this IServiceCollection services)
    {
        services.AddLovelyGitJsonTypeInfoResolver(OperationStateJsonSerializerContext.Default);
        services.AddSingleton<GitOperationStateService>();
        services.AddSingleton<ICommandResponder, GetGitOperationStateCommandResolver>();

        return services;
    }
}
