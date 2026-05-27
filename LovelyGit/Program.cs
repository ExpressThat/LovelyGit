using AutoDependencyRegistration;
using ExpressThat.LovelyGit.Services.Data;
using ExpressThat.LovelyGit.Services.Data.Models;
using ExpressThat.LovelyGit.Services.Data.Repositorys;
using ExpressThat.LovelyGit.Services.Git.CommitGraph;
using ExpressThat.LovelyGit.Services.Git.CommitGraph.Models;
using InfiniFrame;
using InfiniFrame.WebServer;
using KeySharp;
using Microsoft.AspNetCore.Mvc;
using System.Drawing;
using System.Text;
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
        appBuilder.Services.AddSingleton<AppDbContext>();
        

        appBuilder.Services.AutoRegisterDependencies();


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

        ClearCommitGraphTransientState(application.WebApp.Services, CancellationToken.None)
            .GetAwaiter()
            .GetResult();
        application.WebApp.Lifetime.ApplicationStopping.Register(() =>
            ClearCommitGraphTransientState(application.WebApp.Services, CancellationToken.None)
                .GetAwaiter()
                .GetResult());

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

        application.WebApp.MapPost("/commitGraph", async (
            CommitGraphPageRequest request,
            CommitGraphRepository commitGraphRepository,
            KnownGitRepositorysRepository knownGitRepositorysRepository,
            CancellationToken cancellationToken
            ) =>
        {

            var dotGitPath = @"C:\Projects\git";

            KnownGitRepository? foundRepo = null;

            foreach (KnownGitRepository repo in await knownGitRepositorysRepository.GetAllAsync())
            {
                if (repo.Path == dotGitPath)
                {
                    foundRepo = repo;
                }
            }

            if (foundRepo == null)
            {
                foundRepo = await knownGitRepositorysRepository.AddAsync(new KnownGitRepository
                {
                    Id = Guid.NewGuid(),
                    Name = "Local Git Repository",
                    Path = dotGitPath,
                });
            }

            var repositoryGraphId = foundRepo.Id.ToString("N");
            var limit = request.Limit;
            if (limit < 0) limit = 0;
            var cursorState = DecodeCursorState(request.Cursor);

            try
            {
                var openResult = await CommitGraphNative.TryOpenAsync(
                    dotGitPath,
                    repositoryGraphId,
                    commitGraphRepository,
                    cancellationToken);
                if (!openResult.Success || openResult.Graph == null)
                {
                    return Results.Json(
                        new ApiErrorResponse
                        {
                            Error = openResult.Error ?? "Failed to open native commit-graph.",
                        },
                        AppJsonSerializerContext.Default.ApiErrorResponse,
                        statusCode: StatusCodes.Status500InternalServerError);
                }

                using var graph = openResult.Graph;
                var page = await graph.GetCommitGraphPageAsync(cursorState, limit, cancellationToken);
                var response = page.Response;
                response.NextCursor = response.HasMore ? EncodeCursorState(page.NextCursor) : null;
                return Results.Json(response, AppJsonSerializerContext.Default.CommitGraphResponse);
            }
            catch (Exception ex)
            {
                return Results.Json(
                    new ApiErrorResponse
                    {
                        Error = ex.Message,
                    },
                    AppJsonSerializerContext.Default.ApiErrorResponse,
                    statusCode: StatusCodes.Status500InternalServerError);
            }
        });

        application.UseAutoServerClose();

        application.WebApp.UseStaticFiles();
        application.WebApp.MapStaticAssets();

        application.Run();

    }
    private static CommitGraphCursorState DecodeCursorState(string? cursor)
    {
        if (string.IsNullOrWhiteSpace(cursor))
        {
            return new CommitGraphCursorState(null, 0);
        }

        var parts = cursor.Split(':', 2);
        if (parts.Length == 2 && int.TryParse(parts[1], out var offset))
        {
            return new CommitGraphCursorState(parts[0], offset);
        }

        return new CommitGraphCursorState(cursor, 0);
    }

    private static string EncodeCursorState(CommitGraphCursorState cursor)
    {
        return cursor.RepositoryId == null ? string.Empty : $"{cursor.RepositoryId}:{cursor.Offset}";
    }

    private static Task ClearCommitGraphTransientState(
        IServiceProvider services,
        CancellationToken cancellationToken)
    {
        return services
            .GetRequiredService<CommitGraphRepository>()
            .ClearTransientGraphStateAsync(cancellationToken);
    }
}


internal record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}

internal record ApiErrorResponse
{
    public string Error { get; set; } = string.Empty;
}

internal record CommitGraphPageRequest
{
    public int Limit { get; set; }
    public string? Cursor { get; set; }
}

[JsonSerializable(typeof(WeatherForecast[]))]
[JsonSerializable(typeof(ApiErrorResponse))]
[JsonSerializable(typeof(CommitGraphPageRequest))]
[JsonSerializable(typeof(CommitStats))]
[JsonSerializable(typeof(CommitInfo))]
[JsonSerializable(typeof(CommitLaneEdge))]
[JsonSerializable(typeof(CommitGraphRow))]
[JsonSerializable(typeof(CommitGraphResponse))]
internal partial class AppJsonSerializerContext : JsonSerializerContext
{
}
