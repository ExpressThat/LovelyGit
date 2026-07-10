using ExpressThat.LovelyGit.Services.Git.Branches;
using ExpressThat.LovelyGit.Services.Git.Checkout;
using ExpressThat.LovelyGit.Services.Git.CherryPick;
using ExpressThat.LovelyGit.Services.Git.Revert;
using ExpressThat.LovelyGit.Services.Git.Reset;
using ExpressThat.LovelyGit.Services.Git.Stashes;
using ExpressThat.LovelyGit.Services.Git.Tags;

namespace ExpressThat.LovelyGit.Services.Git.Cli;

internal static class GitCliServiceCollectionExtensions
{
    public static IServiceCollection AddLovelyGitGitCli(this IServiceCollection services)
    {
        services.AddSingleton<GitCliService>();
        services.AddSingleton<GitOperationService>();
        services.AddSingleton<GitRemoteCommandService>();
        services.AddSingleton<GitBranchCommandService>();
        services.AddSingleton<GitCheckoutCommandService>();
        services.AddSingleton<GitCherryPickCommandService>();
        services.AddSingleton<GitRepositoryOperationService>();
        services.AddSingleton<GitStashCommandService>();
        services.AddSingleton<GitRevertCommandService>();
        services.AddSingleton<GitResetCommandService>();
        services.AddSingleton<GitTagCommandService>();
        services.AddSingleton<GitCloneService>();
        return services;
    }
}
