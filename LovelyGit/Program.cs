using ExpressThat.LovelyGit.Services;
using ExpressThat.LovelyGit.Services.Data;
using ExpressThat.LovelyGit.Services.Dialogs;
using ExpressThat.LovelyGit.Services.Hubs;
using ExpressThat.LovelyGit.Services.Keyring;
using InfiniFrame;
using InfiniFrame.WebServer;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using Velopack;
using Velopack.Sources;

namespace ExpressThat.LovelyGit;

public static class Program
{
    private const string GitHubRepositoryUrl = "https://github.com/ExpressThat/LovelyGit";
    private const bool IncludePrereleases = true;

    [STAThread]
    public static void Main(string[] args)
    {
#if !DEBUG
        Directory.SetCurrentDirectory(AppContext.BaseDirectory);
#endif
        GitRepoCacheDbContext.ClearCache();
        VelopackApp.Build().Run();
        CheckForUpdatesAtStartup(args);

        InfiniFrameWebApplicationBuilder appBuilder = InfiniFrameWebApplication.CreateBuilder(args);

        appBuilder.Services.AddLovelyGitServices();


        appBuilder.WindowBuilder
                    .SetUseOsDefaultSize(false)
                    .SetChromeless(false)
                    .SetResizable(true)
                    .Center()
                    .SetTitle("LovelyGit")
                    .SetIconFile(GetWindowIconPath())
                    .SetSize(new Size(800, 600))
#if !DEBUG
                    .SetStartUrl("http://localhost:5000")
#endif
                    ;


        Keyring.GetPassword();

        var application = appBuilder.Build();
        application.WebApp.Services
            .GetRequiredService<InfiniFrameWindowProvider>()
            .SetWindow(application.Window);

        application.WebApp.Lifetime.ApplicationStopping.Register(() =>
        {
            application.WebApp.Services.GetService<GitRepoCacheDbContext>()?.Dispose();
            GitRepoCacheDbContext.ClearCache(registerKeys: false);
        });

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

    private static string GetWindowIconPath()
    {
        var fileName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? "LovelyGit.ico"
            : "LovelyGit.png";

        return Path.Combine(AppContext.BaseDirectory, "Assets", fileName);
    }

    private static void CheckForUpdatesAtStartup(string[] args)
    {
        try
        {
            var source = new GithubSource(
                GitHubRepositoryUrl,
                accessToken: null,
                prerelease: IncludePrereleases,
                downloader: null);

            var updateManager = new UpdateManager(source);
            if (!updateManager.IsInstalled)
            {
                return;
            }

            var pendingUpdate = updateManager.UpdatePendingRestart;
            if (pendingUpdate is not null)
            {
                updateManager.ApplyUpdatesAndRestart(pendingUpdate, args);
                return;
            }

            var updateInfo = updateManager.CheckForUpdates();
            if (updateInfo is null)
            {
                return;
            }

            updateManager.DownloadUpdates(updateInfo, progress: null);
            updateManager.ApplyUpdatesAndRestart(updateInfo.TargetFullRelease, args);
        }
        catch (Exception exception)
        {
            Trace.TraceWarning("Velopack startup update check failed: {0}", exception);
            Console.Error.WriteLine("Velopack startup update check failed: {0}", exception);
        }
    }
}
