using ExpressThat.LovelyGit.Services.Json;
using ExpressThat.LovelyGit.Services.NativeMessaging;

namespace ExpressThat.LovelyGit.Services.NativeMessaging.Commands;

internal static class CommandServiceCollectionExtensions
{
    public static IServiceCollection AddLovelyGitCommands(this IServiceCollection services)
    {
        services.AddLovelyGitJsonTypeInfoResolver(CommandJsonSerializerContext.Default);
        services.AddSingleton<CommandResolver>();

        return services;
    }
}
