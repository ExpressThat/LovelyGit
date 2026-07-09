namespace ExpressThat.LovelyGit.Services.Git.Cli;

using ExpressThat.LovelyGit.Services.Git.Branches;
using ExpressThat.LovelyGit.Services.Git.Checkout;

internal static class GitCliServiceCollectionExtensions
{
    public static IServiceCollection AddLovelyGitGitCli(this IServiceCollection services)
    {
        services.AddSingleton<GitCliService>();
        services.AddSingleton<GitOperationService>();
        services.AddSingleton<GitRemoteCommandService>();
        services.AddSingleton<GitBranchCommandService>();
        services.AddSingleton<GitCheckoutCommandService>();
        return services;
    }
}
