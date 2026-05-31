using ExpressThat.LovelyGit.Services;
using ExpressThat.LovelyGit.Services.Data;
using ExpressThat.LovelyGit.Services.Hubs;
using InfiniFrame;
using InfiniFrame.WebServer;
using KeySharp;
using System.Drawing;
using System.Runtime.InteropServices;
using Velopack;

namespace ExpressThat.LovelyGit;

public static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        Directory.SetCurrentDirectory(AppContext.BaseDirectory);

        GitRepoCacheDbContext.ClearCache();
        VelopackApp.Build().Run();

        InfiniFrameWebApplicationBuilder appBuilder = InfiniFrameWebApplication.CreateBuilder(args);

        RegisterDependencies.Register(appBuilder.Services);


        appBuilder.WindowBuilder
                    .SetUseOsDefaultSize(false)
                    .SetResizable(true)
                    .Center()
                    .SetTitle("LovelyGit")
                    .SetIconFile(GetWindowIconPath())
                    .SetSize(new Size(800, 600))
#if !DEBUG
                    .SetStartUrl("http://localhost:5000")
#endif
                    ;


        Keyring.SetPassword("expressthat.lovelygit", "Security", "MasterPassword", "password");

        var application = appBuilder.Build();

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
}
