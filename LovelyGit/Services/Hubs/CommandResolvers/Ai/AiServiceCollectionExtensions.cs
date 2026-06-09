using ExpressThat.LovelyGit.Services.Ai;
using ExpressThat.LovelyGit.Services.Hubs.Commands;
using ExpressThat.LovelyGit.Services.Json;

namespace ExpressThat.LovelyGit.Services.Hubs.CommandResolvers.Ai;

internal static class AiServiceCollectionExtensions
{
    public static IServiceCollection AddAiCommands(this IServiceCollection services)
    {
        services.AddLovelyGitJsonTypeInfoResolver(AiJsonSerializerContext.Default);
        services.AddHttpClient<AiModelDownloadService>();
        services.AddSingleton<AiCommitMessageService>();
        services.AddSingleton<ICommandResponder, GenerateCommitMessageCommandResolver>();
        services.AddSingleton<ICommandResponder, GetAiModelLicensesCommandResolver>();

        return services;
    }
}
