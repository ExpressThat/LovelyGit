using ExpressThat.LovelyGit.Services.Git.Checkout;
using ExpressThat.LovelyGit.Services.Json;
using ExpressThat.LovelyGit.Services.NativeMessaging.Commands;

namespace ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.Checkout;

internal static class CheckoutServiceCollectionExtensions
{
    public static IServiceCollection AddCheckoutCommands(this IServiceCollection services)
    {
        services.AddLovelyGitJsonTypeInfoResolver(CheckoutJsonSerializerContext.Default);
        services.AddSingleton<GitCheckoutCommandService>();
        services.AddSingleton<ICommandResponder, CheckoutBranchCommandResolver>();
        services.AddSingleton<ICommandResponder, CheckoutCommitDetachedCommandResolver>();
        return services;
    }
}
