using BLite.Core;
using BLite.Core.Collections;
using ExpressThat.LovelyGit.Services.Data.Models;

namespace ExpressThat.LovelyGit.Services.Data;

public partial class AppDbContext : DocumentDbContext
{
    public DocumentCollection<Guid, KnownGitRepository> KnownGitRepositorys { get; set; } = null!;

    public DocumentCollection<string, KnownGitRepositoryOrder> KnownGitRepositoryOrders { get; set; } = null!;

    public DocumentCollection<string, SettingModel> Settings { get; set; } = null!;

    public AppDbContext() : base(GetBasePath())
    {
        InitializeCollections();
    }

    public static void RegisterBsonKeys()
    {
        using var engine = new BLiteEngine(GetBasePath());
        engine.RegisterKeys(BsonKeys);
    }

    public static string GetBasePath()
    {
        var dataDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "LovelyGit");
        Directory.CreateDirectory(dataDirectory);
        return Path.Combine(dataDirectory, "LovelyGit.blite");
    }
}
