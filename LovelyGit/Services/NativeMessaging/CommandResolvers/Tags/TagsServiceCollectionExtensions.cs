using ExpressThat.LovelyGit.Services.Git.Tags;
using ExpressThat.LovelyGit.Services.Json;
using ExpressThat.LovelyGit.Services.NativeMessaging.Commands;

namespace ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.Tags;

internal static class TagsServiceCollectionExtensions
{
    public static IServiceCollection AddTagCommands(this IServiceCollection services)
    {
        services.AddLovelyGitJsonTypeInfoResolver(TagsJsonSerializerContext.Default);
        services.AddSingleton<GitTagCommandService>();
        services.AddSingleton<ICommandResponder, CreateTagAtCommitCommandResolver>();
        return services;
    }
}
