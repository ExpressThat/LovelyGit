using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ExpressThat.LovelyGit.Services.Data.Models.Git.CommitGraph
{
    [Table("commit_graph_state")]
    public sealed record CommitGraphRepositoryState
    {
        [Key]
        public string Id { get; set; } = string.Empty;

        [Column("repositoryid")]
        public string RepositoryId { get; set; } = string.Empty;

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
        public string RepositoryId { get; set; } = string.Empty;

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
        public string RepositoryId { get; set; } = string.Empty;

        [Column("hash")]
        public string Hash { get; set; } = string.Empty;
    }
}
