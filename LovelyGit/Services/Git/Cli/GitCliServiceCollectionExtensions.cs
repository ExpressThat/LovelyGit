using ExpressThat.LovelyGit.Services.Git.Branches;
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
        services.AddSingleton<GitRepositoryOperationService>();
        services.AddSingleton<GitStashCommandService>();
        services.AddSingleton<GitTagCommandService>();
        services.AddSingleton<GitCloneService>();
        return services;
    }
}
