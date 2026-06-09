using ExpressThat.LovelyGit.Services.Data;
using ExpressThat.LovelyGit.Services.Dialogs;
using ExpressThat.LovelyGit.Services.Git.Cli;
using ExpressThat.LovelyGit.Services.Hubs.CommandResolvers.Ai;
using ExpressThat.LovelyGit.Services.Hubs.CommandResolvers.CommitGraph;
using ExpressThat.LovelyGit.Services.Hubs.CommandResolvers.KnownRepository;
using ExpressThat.LovelyGit.Services.Hubs.CommandResolvers.Settings;
using ExpressThat.LovelyGit.Services.Hubs.CommandResolvers.WorkingTree;
using ExpressThat.LovelyGit.Services.Hubs.Commands;
using ExpressThat.LovelyGit.Services.Json;

namespace ExpressThat.LovelyGit.Services
{
    public static class RegisterDependencies
    {
        public static IServiceCollection AddLovelyGitServices(this IServiceCollection services)
        {
            services
                .AddSignalR(options =>
                {
                    options.EnableDetailedErrors = true;
                })
                .AddJsonProtocol();

            services
                .AddLovelyGitJsonDefaults()
                .AddLovelyGitData()
                .AddLovelyGitDialogs()
                .AddLovelyGitGitCli()
                .AddLovelyGitCommands()
                .AddKnownRepositoryCommands()
                .AddCommitGraphCommands()
                .AddAiCommands()
                .AddWorkingTreeCommands()
                .AddSettingsCommands();

            return services;
        }
    }
}
