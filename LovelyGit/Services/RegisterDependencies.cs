using ExpressThat.LazyGit;
using ExpressThat.LovelyGit.Services.Data;
using ExpressThat.LovelyGit.Services.Data.Repositorys;
using ExpressThat.LovelyGit.Services.Hubs.CommandResolvers;
using ExpressThat.LovelyGit.Services.Hubs.CommandResolvers.CommitGraph;
using ExpressThat.LovelyGit.Services.Hubs.CommandResolvers.KnownRepository;
using ExpressThat.LovelyGit.Services.Hubs.CommandResolvers.Settings;
using ExpressThat.LovelyGit.Services.Hubs.Commands;
using ExpressThat.LovelyGit.Services.Settings;
using System.Text.Json.Serialization;

namespace ExpressThat.LovelyGit.Services
{
    public class RegisterDependencies
    {
        public static void Register(IServiceCollection services)
        {
            services.ConfigureHttpJsonOptions(options =>
            {
                options.SerializerOptions.TypeInfoResolverChain.Insert(0, AppJsonSerializerContext.Default);
                options.SerializerOptions.TypeInfoResolverChain.Insert(0, CommandReponseJsonSerializerContext.Default);
                options.SerializerOptions.Converters.Add(new JsonStringEnumConverter<CommsHubCommandType>());
                options.SerializerOptions.Converters.Add(new JsonStringEnumConverter<Setting>());
            });

            services
            .AddSignalR(options =>
            {
                options.EnableDetailedErrors = true;
            })
            .AddJsonProtocol(options =>
            {
                options.PayloadSerializerOptions.TypeInfoResolverChain.Insert(0, AppJsonSerializerContext.Default);
                options.PayloadSerializerOptions.TypeInfoResolverChain.Insert(0, CommandReponseJsonSerializerContext.Default);
                options.PayloadSerializerOptions.Converters.Add(new JsonStringEnumConverter<CommsHubCommandType>());
                options.PayloadSerializerOptions.Converters.Add(new JsonStringEnumConverter<Setting>());
            });

            services.AddSingleton<AppDbContext>();
            services.AddSingleton<KnownGitRepositorysRepository>();
            services.AddSingleton<SettingsManager>();

            //Commands 
            services.AddSingleton<CommandResolver>();
            services.AddSingleton<ICommandResponder, KnownGitRepositorysCommandResolver>();
            services.AddSingleton<ICommandResponder, CommitGraphCommandResolver>();
            services.AddSingleton<ICommandResponder, GetSettingsCommandResolver>();
            services.AddSingleton<ICommandResponder, SetSettingsCommandResolver>();
            services.AddSingleton<ICommandResponder, GetAllSettingsCommandResolver>();
        }
    }
}
