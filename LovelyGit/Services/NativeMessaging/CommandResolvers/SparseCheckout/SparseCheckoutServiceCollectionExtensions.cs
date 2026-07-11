using ExpressThat.LovelyGit.Services.Git.SparseCheckout;
using ExpressThat.LovelyGit.Services.Json;
using ExpressThat.LovelyGit.Services.NativeMessaging.Commands;

namespace ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.SparseCheckout;

internal static class SparseCheckoutServiceCollectionExtensions
{
    public static IServiceCollection AddSparseCheckoutCommands(this IServiceCollection services)
    {
        services.AddLovelyGitJsonTypeInfoResolver(SparseCheckoutJsonSerializerContext.Default);
        services.AddSingleton<NativeSparseCheckoutReader>();
        services.AddSingleton<GitSparseCheckoutCommandService>();
        services.AddSingleton<ICommandResponder, GetSparseCheckoutStateCommandResolver>();
        services.AddSingleton<ICommandResponder, ManageSparseCheckoutCommandResolver>();
        return services;
    }
}
