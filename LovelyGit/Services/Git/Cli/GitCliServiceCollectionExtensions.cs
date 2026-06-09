namespace ExpressThat.LovelyGit.Services.Git.Cli;

internal static class GitCliServiceCollectionExtensions
{
    public static IServiceCollection AddLovelyGitGitCli(this IServiceCollection services)
    {
        services.AddSingleton<GitCliService>();
        services.AddSingleton<GitRemoteCommandService>();
        return services;
    }
}
