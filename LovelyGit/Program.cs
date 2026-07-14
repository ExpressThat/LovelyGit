using ExpressThat.LovelyGit.Services;
using ExpressThat.LovelyGit.Services.Data;
using ExpressThat.LovelyGit.Services.Dialogs;
using ExpressThat.LovelyGit.Services.Git.CommitGraph;
using ExpressThat.LovelyGit.Services.NativeMessaging;
using ExpressThat.LovelyGit.Services.Keyring;
using ExpressThat.LovelyGit.Services.Updates;
using InfiniFrame;
using InfiniFrame.WebServer;
using System.Drawing;
using System.Runtime.InteropServices;
using Velopack;

namespace ExpressThat.LovelyGit;

public static class Program
{
    private const bool EnableCommitGraphCacheWorker = false;
    private const bool EnableCommitDetailsPreloadWorker = false;
    private const bool EnableCommitFileDiffPreparationWorker = false;
    private const string TestWindowOffscreenEnvironmentVariable = "LOVELYGIT_TEST_WINDOW_OFFSCREEN";
    private const string TestWindowWidthEnvironmentVariable = "LOVELYGIT_TEST_WINDOW_WIDTH";
    private const string TestWindowHeightEnvironmentVariable = "LOVELYGIT_TEST_WINDOW_HEIGHT";
    private static readonly Size DefaultWindowSize = new(800, 600);

    [STAThread]
    public static void Main(string[] args)
    {
#if !DEBUG
        Directory.SetCurrentDirectory(AppContext.BaseDirectory);
#endif
        AppDbContext.RegisterBsonKeys();
        GitRepoCacheDbContext.EnsureCacheReady();
        VelopackApp.Build().Run();
        var startupUpdates = StartupUpdateCoordinator.Create(() => new VelopackUpdateClient());
        startupUpdates.ApplyPendingUpdate(args);

        InfiniFrameWebApplicationBuilder appBuilder = InfiniFrameWebApplication.CreateBuilder(args);

        appBuilder.Services.AddSingleton(startupUpdates);
        appBuilder.Services.AddHostedService<StartupUpdateBackgroundService>();
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
                    .SetSize(GetInitialWindowSize())
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

    private static Size GetInitialWindowSize()
    {
        return new Size(
            GetWindowDimension(TestWindowWidthEnvironmentVariable, DefaultWindowSize.Width),
            GetWindowDimension(TestWindowHeightEnvironmentVariable, DefaultWindowSize.Height));
    }

    private static int GetWindowDimension(string environmentVariable, int fallback)
    {
        var value = Environment.GetEnvironmentVariable(environmentVariable);
        return int.TryParse(value, out var parsed) && parsed >= 400
            ? parsed
            : fallback;
    }
}
