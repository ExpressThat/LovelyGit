using ExpressThat.LovelyGit.Services.Json;

namespace ExpressThat.LovelyGit.Services.Hubs.Commands;

internal static class CommandServiceCollectionExtensions
{
    public static IServiceCollection AddLovelyGitCommands(this IServiceCollection services)
    {
        services.AddLovelyGitJsonTypeInfoResolver(CommandJsonSerializerContext.Default);
        services.AddSingleton<CommandResolver>();

        return services;
    }
}
