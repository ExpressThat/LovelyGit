using ExpressThat.LovelyGit.Services.NativeMessaging.Commands;
using ExpressThat.LovelyGit.Services.Json;
using ExpressThat.LovelyGit.Services.Settings;

namespace ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.Settings;

internal static class SettingsServiceCollectionExtensions
{
    public static IServiceCollection AddSettingsCommands(this IServiceCollection services)
    {
        services.AddLovelyGitJsonTypeInfoResolver(SettingsJsonSerializerContext.Default);
        services.AddSingleton<SettingsManager>();
        services.AddSingleton<ICommandResponder, GetSettingsCommandResolver>();
        services.AddSingleton<ICommandResponder, SetSettingsCommandResolver>();
        services.AddSingleton<ICommandResponder, GetAllSettingsCommandResolver>();
        services.AddSingleton<ICommandResponder, SetMultipleSettingsCommandResolver>();

        return services;
    }
}
