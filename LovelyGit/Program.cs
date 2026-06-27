using ExpressThat.LovelyGit.Services;
using ExpressThat.LovelyGit.Services.Data;
using ExpressThat.LovelyGit.Services.Dialogs;
using ExpressThat.LovelyGit.Services.Git.CommitGraph;
using ExpressThat.LovelyGit.Services.NativeMessaging;
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
    private const bool EnableCommitGraphCacheWorker = false;
    private const bool EnableCommitDetailsPreloadWorker = false;
    private const bool EnableCommitFileDiffPreparationWorker = false;
    private const string TestWindowOffscreenEnvironmentVariable = "LOVELYGIT_TEST_WINDOW_OFFSCREEN";

    [STAThread]
    public static void Main(string[] args)
    {
#if !DEBUG
        Directory.SetCurrentDirectory(AppContext.BaseDirectory);
#endif
        AppDbContext.RegisterBsonKeys();
        GitRepoCacheDbContext.ClearCache();
        VelopackApp.Build().Run();
        CheckForUpdatesAtStartup(args);

        InfiniFrameWebApplicationBuilder appBuilder = InfiniFrameWebApplication.CreateBuilder(args);

        appBuilder.Services.AddSingleton(new CommitGraphBackgroundWorkerOptions(
            EnableCommitGraphCacheWorker,
            EnableCommitDetailsPreloadWorker,
            EnableCommitFileDiffPreparationWorker));
        appBuilder.Services.AddLovelyGitServices();


        var windowBuilder = appBuilder.WindowBuilder
                    .SetUseOsDefaultSize(false)
                    .SetChromeless(false)
                    .SetResizable(true)
                    .SetTitle("LovelyGit")
                    .SetIconFile(GetWindowIconPath())
                    .SetSize(new Size(800, 600))
                    .SetStartUrl("http://localhost:5000")
                    .UseNativeMessaging()
                    ;

        ApplyInitialWindowPlacement(windowBuilder);

        Keyring.GetPassword();

        var application = appBuilder.Build();
        application.WebApp.Services
            .GetRequiredService<InfiniFrameWindowProvider>()
            .SetWindow(application.Window);

        application.WebApp.Lifetime.ApplicationStopping.Register(() =>
        {
            application.WebApp.Services.GetService<CommitFileDiffService>()?.StopAndWait();
            application.WebApp.Services.GetService<CommitDetailsPreloadService>()?.StopAndWait();
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

    private static void ApplyInitialWindowPlacement(IInfiniFrameWindowBuilder windowBuilder)
    {
        if (IsTestWindowOffscreenEnabled())
        {
            windowBuilder
                .SetUseOsDefaultLocation(false)
                .SetLocation(-32000, -32000)
                .SetMinimized(true);
            return;
        }

        windowBuilder.Center();
    }

    private static bool IsTestWindowOffscreenEnabled()
    {
        var value = Environment.GetEnvironmentVariable(TestWindowOffscreenEnvironmentVariable);
        return bool.TryParse(value, out var enabled) && enabled;
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
