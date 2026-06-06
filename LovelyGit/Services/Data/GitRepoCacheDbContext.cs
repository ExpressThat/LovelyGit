using BLite.Core;
using BLite.Core.Collections;
using ExpressThat.LovelyGit.Services.Data.Models.Git.CommitGraph;

namespace ExpressThat.LovelyGit.Services.Data;

public partial class GitRepoCacheDbContext : DocumentDbContext
{
    private static readonly string[] BsonKeys =
    [
        "id",
        "_id",
        "repositoryid",
        "offset",
        "maxlanecount",
        "lanes",
        "hash",
        "seconds",
        "details",
        "parents",
        "author",
        "email",
        "date",
        "subject",
        "body",
        "message",
        "branches",
        "tags",
        "stats",
        "changedfiles",
        "path",
        "status",
        "additions",
        "deletions",
        "isbinary",
    ];

    public DocumentCollection<Guid, CommitGraphRepositoryState> CommitGraphStates { get; set; } = null!;
    public DocumentCollection<string, CommitGraphFrontierEntry> CommitGraphFrontier { get; set; } = null!;
    public DocumentCollection<string, CommitGraphSeenEntry> CommitGraphSeen { get; set; } = null!;
    public DocumentCollection<string, CommitDetailsCacheEntry> CommitDetailsCache { get; set; } = null!;

    public GitRepoCacheDbContext() : base(GetBasePath())
    {
        InitializeCollections();
    }

    public static void ClearCache(bool registerKeys = true)
    {
        DeleteIfExists(GetBasePath());
        DeleteIfExists(GetBasePath().Replace(".blite", ".wal"));

        if (registerKeys)
        {
            RegisterBsonKeys();
        }
    }

    public static void RegisterBsonKeys()
    {
        using var engine = new BLiteEngine(GetBasePath());
        engine.RegisterKeys(BsonKeys);
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

    protected override void OnModelCreating(BLite.Core.Metadata.ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CommitGraphRepositoryState>()
            .HasIndex(entity => entity.RepositoryId);

        modelBuilder.Entity<CommitGraphFrontierEntry>()
            .HasIndex(entity => entity.RepositoryId);

        modelBuilder.Entity<CommitGraphSeenEntry>()
            .HasIndex(entity => entity.RepositoryId);

        modelBuilder.Entity<CommitDetailsCacheEntry>()
            .HasIndex(entity => entity.RepositoryId);
    }
}
