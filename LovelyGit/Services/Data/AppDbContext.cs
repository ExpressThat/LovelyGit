using AutoDependencyRegistration.Attributes;
using BLite.Core;
using BLite.Core.Collections;
using ExpressThat.LovelyGit.Services.Data.Models;
using ExpressThat.LovelyGit.Services.Data.Models.Git.CommitGraph;

namespace ExpressThat.LovelyGit.Services.Data;

[RegisterClassAsSingleton]
public partial class AppDbContext : DocumentDbContext
{
    public DocumentCollection<string, CommitGraphRepositoryState> CommitGraphStates { get; set; } = null!;
    public DocumentCollection<string, CommitGraphFrontierEntry> CommitGraphFrontier { get; set; } = null!;
    public DocumentCollection<string, CommitGraphSeenEntry> CommitGraphSeen { get; set; } = null!;
    public DocumentCollection<Guid, KnownGitRepository> KnownGitRepositorys { get; set; } = null!;

    public AppDbContext() : base(GetBasePath())
    {
        InitializeCollections();
    }

    private static string GetBasePath()
    {
        var dataDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "LovelyGit");
        Directory.CreateDirectory(dataDirectory);
        return Path.Combine(dataDirectory, "LovelyGit.blite");
    }

    protected override void OnModelCreating(BLite.Core.Metadata.ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CommitGraphRepositoryState>()
            .HasIndex(entity => entity.RepositoryId);

        modelBuilder.Entity<CommitGraphFrontierEntry>()
            .HasIndex(entity => entity.RepositoryId);

        modelBuilder.Entity<CommitGraphSeenEntry>()
            .HasIndex(entity => entity.RepositoryId);
    }
}
