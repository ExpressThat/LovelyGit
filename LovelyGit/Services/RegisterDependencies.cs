using ExpressThat.LovelyGit.Services.Data;
using ExpressThat.LovelyGit.Services.Dialogs;
using ExpressThat.LovelyGit.Services.Git.Cli;
using ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.CommitGraph;
using ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.Bisect;
using ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.Configuration;
using ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.KnownRepository;
using ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.Lfs;
using ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.Settings;
using ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.WorkingTree;
using ExpressThat.LovelyGit.Services.NativeMessaging.Commands;
using ExpressThat.LovelyGit.Services.Json;
using ExpressThat.LovelyGit.Services.NativeMessaging;

namespace ExpressThat.LovelyGit.Services
{
    public static class RegisterDependencies
    {
        public static IServiceCollection AddLovelyGitServices(this IServiceCollection services)
        {
            services
                .AddLovelyGitJsonDefaults()
                .AddNativeMessaging()
                .AddLovelyGitData()
                .AddLovelyGitDialogs()
                .AddLovelyGitGitCli()
                .AddLovelyGitCommands()
                .AddKnownRepositoryCommands()
                .AddCommitGraphCommands()
                .AddBisectCommands()
                .AddConfigurationCommands()
                .AddLfsCommands()
                .AddWorkingTreeCommands()
                .AddSettingsCommands();

            return services;
        }
    }
}
