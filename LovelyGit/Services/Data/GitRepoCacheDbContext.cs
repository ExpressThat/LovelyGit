using BLite.Core;
using BLite.Core.Collections;
using ExpressThat.LovelyGit.Services.Data.Models.Git.CommitGraph;

namespace ExpressThat.LovelyGit.Services.Data;

public partial class GitRepoCacheDbContext : DocumentDbContext
{
    private const string CacheSchemaVersion = "1";
    public DocumentCollection<Guid, CommitGraphRepositoryState> CommitGraphStates { get; set; } = null!;
    public DocumentCollection<string, CommitGraphFrontierEntry> CommitGraphFrontier { get; set; } = null!;
    public DocumentCollection<string, CommitGraphSeenEntry> CommitGraphSeen { get; set; } = null!;
    public DocumentCollection<string, CommitGraphCachedCommitEntry> CommitGraphCachedCommits { get; set; } = null!;
    public DocumentCollection<string, CommitDetailsCacheEntry> CommitDetailsCache { get; set; } = null!;
    public DocumentCollection<string, CommitChangedFileCacheEntry> CommitDetailsChangedFiles { get; set; } = null!;
    public DocumentCollection<string, CommitFileDiffCacheEntry> CommitFileDiffs { get; set; } = null!;
    public DocumentCollection<string, CommitFileDiffLineCacheEntry> CommitFileDiffLines { get; set; } = null!;

    public GitRepoCacheDbContext() : base(GetBasePath())
    {
        InitializeCollections();
    }

    public static void ClearCache(bool registerKeys = true)
    {
        DeleteIfExists(GetBasePath());
        DeleteIfExists(GetBasePath().Replace(".blite", ".wal"));
        DeleteIfExists(GetVersionPath());

        if (registerKeys)
        {
            RegisterBsonKeys();
        }
    }

    public static void EnsureCacheReady()
    {
        var databasePath = GetBasePath();
        var versionPath = GetVersionPath();
        if (CacheDatabaseLifecycle.IsCurrent(
                databasePath,
                versionPath,
                CacheSchemaVersion))
        {
            return;
        }

        ClearCache(registerKeys: false);
        RegisterBsonKeys();
        CacheDatabaseLifecycle.WriteVersion(versionPath, CacheSchemaVersion);
    }

    public static void RegisterBsonKeys()
    {
        using var engine = new BLiteEngine(GetBasePath());
        engine.RegisterKeys(CacheBsonKeys);
    }

    private static void DeleteIfExists(string path)
    {
        const int attempts = 5;
        for (var attempt = 0; attempt < attempts; attempt++)
        {
            try
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }

                return;
            }
            catch (IOException) when (attempt < attempts - 1)
            {
                Thread.Sleep(50);
            }
            catch (UnauthorizedAccessException) when (attempt < attempts - 1)
            {
                Thread.Sleep(50);
            }
        }
    }

    public static string GetBasePath()
    {
        var dataDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "LovelyGit");
        Directory.CreateDirectory(dataDirectory);

        return Path.Combine(dataDirectory, "LovelyGit.Cache.blite");
    }

    private static string GetVersionPath() => GetBasePath() + ".version";

    protected override void OnModelCreating(BLite.Core.Metadata.ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CommitGraphRepositoryState>()
            .HasIndex(entity => entity.RepositoryId);

        modelBuilder.Entity<CommitGraphFrontierEntry>()
            .HasIndex(entity => entity.RepositoryId);

        modelBuilder.Entity<CommitGraphSeenEntry>()
            .HasIndex(entity => entity.RepositoryId);

        modelBuilder.Entity<CommitGraphCachedCommitEntry>()
            .HasIndex(entity => entity.RepositoryId);

        modelBuilder.Entity<CommitDetailsCacheEntry>()
            .HasIndex(entity => entity.RepositoryId);

        modelBuilder.Entity<CommitChangedFileCacheEntry>()
            .HasIndex(entity => entity.RepositoryId);

        modelBuilder.Entity<CommitFileDiffCacheEntry>()
            .HasIndex(entity => entity.RepositoryId);

    }
}
