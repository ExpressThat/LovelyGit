using ExpressThat.LovelyGit.Services.Git;
using InfiniFrame;
using InfiniFrame.WebServer;
using KeySharp;
using Microsoft.AspNetCore.Mvc;
using System.Drawing;
using System.Text.Json.Serialization;

namespace ExpressThat.LazyGit;

public static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        InfiniFrameWebApplicationBuilder appBuilder = InfiniFrameWebApplication.CreateBuilder(args);


        // Add services to the container.
        appBuilder.Services.ConfigureHttpJsonOptions(options =>
        {
            options.SerializerOptions.TypeInfoResolverChain.Insert(0, AppJsonSerializerContext.Default);
        });

        appBuilder.Services.AddSingleton<RepositoryManager>();
        appBuilder.Services.AddActivatedSingleton<CommitGraph>();

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

        var manager = application.WebApp.Services.GetService<RepositoryManager>();
        var commitGraph = application.WebApp.Services.GetService<CommitGraph>();

        if (!manager?.IsRepositoryOpened() ?? false)
        {
            if (manager != null)
                manager.OpenRepository(@"C:\Projects\git").GetAwaiter().GetResult();
        }

        commitGraph?.PrimeMetadataAsync().GetAwaiter().GetResult();

        // Configure the HTTP request pipeline.

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

        application.WebApp.MapGet("/commitGraph", async (
            [FromQuery] int limit,
            [FromQuery] int offset,
            RepositoryManager manager,
            CommitGraph commitGraph
            ) =>
        {
            return await commitGraph.GetCommitGraph(offset, limit);
        });

        application.UseAutoServerClose();

        application.WebApp.UseStaticFiles();
        application.WebApp.MapStaticAssets();

        application.Run();

    }
}


internal record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}

[JsonSerializable(typeof(WeatherForecast[]))]
[JsonSerializable(typeof(CommitStats))]
[JsonSerializable(typeof(CommitInfo))]
[JsonSerializable(typeof(CommitLaneEdge))]
[JsonSerializable(typeof(CommitGraphRow))]
[JsonSerializable(typeof(CommitGraphResponse))]
internal partial class AppJsonSerializerContext : JsonSerializerContext
{
}