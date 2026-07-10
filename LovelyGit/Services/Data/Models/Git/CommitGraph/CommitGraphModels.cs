using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ExpressThat.LovelyGit.Services.Data.Models.Git.CommitGraph
{
    [Table("commit_graph_state")]
    public sealed record CommitGraphRepositoryState
    {
        [Key]
        public Guid Id { get; set; }

        [Column("repositoryid")]
        public Guid RepositoryId { get; set; }

        [Column("offset")]
        public int Offset { get; set; }

        [Column("maxlanecount")]
        public int MaxLaneCount { get; set; }

        [Column("lanes")]
        public string Lanes { get; set; } = string.Empty;
    }

    [Table("commit_graph_frontier")]
    public sealed record CommitGraphFrontierEntry
    {
        [Key]
        public string Id { get; set; } = string.Empty;

        [Column("repositoryid")]
        public Guid RepositoryId { get; set; }

        [Column("hash")]
        public string Hash { get; set; } = string.Empty;

        [Column("seconds")]
        public long Seconds { get; set; }
    }

    [Table("commit_graph_seen")]
    public sealed record CommitGraphSeenEntry
    {
        [Key]
        public string Id { get; set; } = string.Empty;

        [Column("repositoryid")]
        public Guid RepositoryId { get; set; }

        [Column("hash")]
        public string Hash { get; set; } = string.Empty;
    }

    [Table("commit_graph_cached_commit")]
    public sealed record CommitGraphCachedCommitEntry
    {
        [Key]
        public string Id { get; set; } = string.Empty;

        [Column("repositoryid")]
        public Guid RepositoryId { get; set; }

        [Column("rowindex")]
        public int RowIndex { get; set; }

        [Column("hash")]
        public string Hash { get; set; } = string.Empty;
    }

    [Table("commit_details_cache")]
    public sealed record CommitDetailsCacheEntry
    {
        [Key]
        public string Id { get; set; } = string.Empty;

        [Column("repositoryid")]
        public Guid RepositoryId { get; set; }

        [Column("hash")]
        public string Hash { get; set; } = string.Empty;

        [Column("details")]
        public CommitDetailsCache Details { get; set; } = new();
    }

    public sealed record CommitDetailsCache
    {
        [Column("hash")]
        public string Hash { get; set; } = string.Empty;

        [Column("parents")]
        public List<string> Parents { get; set; } = new();

        [Column("author")]
        public string Author { get; set; } = string.Empty;

        [Column("email")]
        public string Email { get; set; } = string.Empty;

        [Column("date")]
        public long Date { get; set; }

        [Column("subject")]
        public string Subject { get; set; } = string.Empty;

        [Column("body")]
        public string Body { get; set; } = string.Empty;

        [Column("message")]
        public string Message { get; set; } = string.Empty;

        [Column("branches")]
        public List<string> Branches { get; set; } = new();

        [Column("tags")]
        public List<string> Tags { get; set; } = new();

        [Column("stats")]
        public CommitStatsCache Stats { get; set; } = new();
    }

    public sealed record CommitStatsCache
    {
        [Column("additions")]
        public long Additions { get; set; }

        [Column("deletions")]
        public long Deletions { get; set; }
    }

    [Table("commit_details_changed_file_cache")]
    public sealed record CommitChangedFileCacheEntry
    {
        [Key]
        public string Id { get; set; } = string.Empty;

        [Column("repositoryid")]
        public Guid RepositoryId { get; set; }

        [Column("hash")]
        public string Hash { get; set; } = string.Empty;

        [Column("fileindex")]
        public int FileIndex { get; set; }

        [Column("file")]
        public CommitChangedFileCache File { get; set; } = new();
    }

    public sealed record CommitChangedFileCache
    {
        [Column("path")]
        public string Path { get; set; } = string.Empty;

        [Column("status")]
        public string Status { get; set; } = string.Empty;

        [Column("additions")]
        public long Additions { get; set; }

        [Column("deletions")]
        public long Deletions { get; set; }

        [Column("isbinary")]
        public bool IsBinary { get; set; }
    }

}
