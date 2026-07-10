using ExpressThat.LovelyGit.Services.Git.Configuration;
using ExpressThat.LovelyGit.Services.Json;
using ExpressThat.LovelyGit.Services.NativeMessaging.Commands;

namespace ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.Configuration;

internal static class ConfigurationServiceCollectionExtensions
{
    public static IServiceCollection AddConfigurationCommands(this IServiceCollection services)
    {
        services.AddLovelyGitJsonTypeInfoResolver(ConfigurationJsonSerializerContext.Default);
        services.AddSingleton<NativeGitCommitIdentityReader>();
        services.AddSingleton<GitCommitIdentityCommandService>();
        services.AddSingleton<ICommandResponder, GetCommitIdentityCommandResolver>();
        services.AddSingleton<ICommandResponder, ManageCommitIdentityCommandResolver>();
        return services;
    }
}
