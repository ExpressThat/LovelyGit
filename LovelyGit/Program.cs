using BLite.Core;
using ExpressThat.LovelyGit.Services.Data;
using ExpressThat.LovelyGit.Services.Data.Models;
using ExpressThat.LovelyGit.Services.Data.Repositorys;
using ExpressThat.LovelyGit.Services.Git.CommitGraph;
using ExpressThat.LovelyGit.Services.Hubs;
using ExpressThat.LovelyGit.Services.Hubs.CommandResolvers;
using ExpressThat.LovelyGit.Services.Hubs.CommandResolvers.CommitGraph;
using ExpressThat.LovelyGit.Services.Hubs.CommandResolvers.KnownRepository;
using ExpressThat.LovelyGit.Services.Hubs.Commands;
using InfiniFrame;
using InfiniFrame.WebServer;
using KeySharp;
using Microsoft.AspNetCore.Mvc;
using System.Drawing;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ExpressThat.LazyGit;

public static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        GitRepoCacheDbContext.ClearCache();
        RegisterBsonKeys(
            AppDbContext.GetBasePath(),
            "id",
            "_id",
            "name",
            "path");
        GitRepoCacheDbContext.RegisterBsonKeys();

        InfiniFrameWebApplicationBuilder appBuilder = InfiniFrameWebApplication.CreateBuilder(args);


        // Add services to the container.
        appBuilder.Services.ConfigureHttpJsonOptions(options =>
        {
            options.SerializerOptions.TypeInfoResolverChain.Insert(0, AppJsonSerializerContext.Default);
            options.SerializerOptions.TypeInfoResolverChain.Insert(0, CommandReponseJsonSerializerContext.Default);
            options.SerializerOptions.Converters.Add(new JsonStringEnumConverter<CommsHubCommandType>());
            options.SerializerOptions.Converters.Add(new JsonStringEnumConverter<CommsHubSubCommandType>());
        });

        appBuilder.Services.AddSingleton<AppDbContext>();
        appBuilder.Services.AddSingleton<KnownGitRepositorysRepository>();
        appBuilder.Services.AddSingleton<CommandResolver>();
        appBuilder.Services.AddSingleton<ICommandResponder, KnownGitRepositorysCommandResolver>();
        appBuilder.Services.AddSingleton<ICommandResponder, CommitGraphCommandResolver>();

        appBuilder.Services
            .AddSignalR(options =>
            {
                options.EnableDetailedErrors = true;
            })
            .AddJsonProtocol(options =>
            {
                options.PayloadSerializerOptions.TypeInfoResolverChain.Insert(0, AppJsonSerializerContext.Default);
                options.PayloadSerializerOptions.TypeInfoResolverChain.Insert(0, CommandReponseJsonSerializerContext.Default);
                options.PayloadSerializerOptions.Converters.Add(new JsonStringEnumConverter<CommsHubCommandType>());
                options.PayloadSerializerOptions.Converters.Add(new JsonStringEnumConverter<CommsHubSubCommandType>());
            });


        appBuilder.WindowBuilder
                    .SetUseOsDefaultSize(false)
                    .SetResizable(true)
                    .Center()
                    .SetTitle("InfiniLore InfiniFrame.NET REACT Sample")
                    .SetSize(new Size(800, 600))
#if !DEBUG
                    .SetStartUrl("http://localhost:5000")
#endif
                    ;


        Keyring.SetPassword("expressthat.lazygit", "Security", "MasterPassword", "password");

        var application = appBuilder.Build();

        application.WebApp.Lifetime.ApplicationStopping.Register(() =>
        {
            application.WebApp.Services.GetService<GitRepoCacheDbContext>()?.Dispose();
            GitRepoCacheDbContext.ClearCache(registerKeys: false);
        });

        var summaries = new[]
        {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };

        application.WebApp.MapGet("/", async (IWebHostEnvironment env) =>
        {
            var htmlPath = Path.Combine(env.WebRootPath ?? string.Empty, "index.html");
            if (!File.Exists(htmlPath))
            {
                return Results.NotFound($"Missing file: {htmlPath}");
            }

            var html = await File.ReadAllTextAsync(htmlPath);
            return Results.Content(html, "text/html; charset=utf-8");
        });

        application.UseAutoServerClose();

        application.WebApp.MapHub<CommsHub>("/commsHub");

        application.WebApp.UseStaticFiles();
        application.WebApp.MapStaticAssets();

        application.Run();

    }

    private static void RegisterBsonKeys(string path, params string[] keys)
    {
        using var engine = new BLiteEngine(path);
        engine.RegisterKeys(keys);
    }
}
