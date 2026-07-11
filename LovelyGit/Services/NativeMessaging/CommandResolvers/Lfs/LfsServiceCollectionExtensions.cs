using ExpressThat.LovelyGit.Services.Git.Lfs;
using ExpressThat.LovelyGit.Services.Json;
using ExpressThat.LovelyGit.Services.NativeMessaging.Commands;

namespace ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.Lfs;

internal static class LfsServiceCollectionExtensions
{
    public static IServiceCollection AddLfsCommands(this IServiceCollection services)
    {
        services.AddLovelyGitJsonTypeInfoResolver(LfsJsonSerializerContext.Default);
        services.AddSingleton<NativeGitLfsStateReader>();
        services.AddSingleton<GitLfsCommandService>();
        services.AddSingleton<ICommandResponder, GetGitLfsStateCommandResolver>();
        services.AddSingleton<ICommandResponder, ManageGitLfsCommandResolver>();
        return services;
    }
}
