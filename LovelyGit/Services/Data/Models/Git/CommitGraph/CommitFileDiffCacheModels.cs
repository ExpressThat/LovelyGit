using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ExpressThat.LovelyGit.Services.Data.Models.Git.CommitGraph
{
    [Table("commit_file_diff_cache")]
    public sealed record CommitFileDiffCacheEntry
    {
        [Key]
        public string Id { get; set; } = string.Empty;
        [Column("repositoryid")]
        public Guid RepositoryId { get; set; }
        [Column("hash")]
        public string Hash { get; set; } = string.Empty;
        [Column("path")]
        public string Path { get; set; } = string.Empty;
        [Column("viewmode")]
        public string ViewMode { get; set; } = string.Empty;
        [Column("ignorewhitespace")]
        public bool IgnoreWhitespace { get; set; }
        [Column("status")]
        public string Status { get; set; } = string.Empty;
        [Column("isbinary")]
        public bool IsBinary { get; set; }
        [Column("hasdifferences")]
        public bool HasDifferences { get; set; }
        [Column("linecount")]
        public int LineCount { get; set; }
        [Column("istruncated")]
        public bool IsTruncated { get; set; }
        [Column("truncationmessage")]
        public string TruncationMessage { get; set; } = string.Empty;
        [Column("virtualtext")]
        public string VirtualText { get; set; } = string.Empty;
        [Column("virtualtextgzipbase64")]
        public string VirtualTextGzipBase64 { get; set; } = string.Empty;
        [Column("virtualtextencoding")]
        public string VirtualTextEncoding { get; set; } = string.Empty;
        [Column("virtualchangetype")]
        public string VirtualChangeType { get; set; } = string.Empty;
        [Column("virtuallinecount")]
        public int VirtualLineCount { get; set; }
        [Column("compactlineschema")]
        public string CompactLineSchema { get; set; } = string.Empty;
        [Column("compactlinesgzipbase64")]
        public string CompactLinesGzipBase64 { get; set; } = string.Empty;
        [Column("compactlinecount")]
        public int CompactLineCount { get; set; }
    }

    [Table("commit_file_diff_line_cache")]
    public sealed record CommitFileDiffLineCacheEntry
    {
        [Key]
        public string Id { get; set; } = string.Empty;
        [Column("repositoryid")]
        public Guid RepositoryId { get; set; }
        [Column("hash")]
        public string Hash { get; set; } = string.Empty;
        [Column("path")]
        public string Path { get; set; } = string.Empty;
        [Column("viewmode")]
        public string ViewMode { get; set; } = string.Empty;
        [Column("ignorewhitespace")]
        public bool IgnoreWhitespace { get; set; }
        [Column("diffid")]
        public string DiffId { get; set; } = string.Empty;
        [Column("lineindex")]
        public int LineIndex { get; set; }
        [Column("line")]
        public CommitFileDiffLineCache Line { get; set; } = new();
    }

    public sealed record CommitFileDiffLineCache
    {
        [Column("oldlinenumber")]
        public int? OldLineNumber { get; set; }
        [Column("newlinenumber")]
        public int? NewLineNumber { get; set; }
        [Column("oldtext")]
        public string OldText { get; set; } = string.Empty;
        [Column("newtext")]
        public string NewText { get; set; } = string.Empty;
        [Column("text")]
        public string Text { get; set; } = string.Empty;
        [Column("changetype")]
        public string ChangeType { get; set; } = string.Empty;
        [Column("oldsyntaxspans")]
        public List<CommitFileDiffSyntaxSpanCache> OldSyntaxSpans { get; set; } = new();
        [Column("newsyntaxspans")]
        public List<CommitFileDiffSyntaxSpanCache> NewSyntaxSpans { get; set; } = new();
        [Column("syntaxspans")]
        public List<CommitFileDiffSyntaxSpanCache> SyntaxSpans { get; set; } = new();
        [Column("oldchangespans")]
        public List<CommitFileDiffChangeSpanCache> OldChangeSpans { get; set; } = new();
        [Column("newchangespans")]
        public List<CommitFileDiffChangeSpanCache> NewChangeSpans { get; set; } = new();
        [Column("changespans")]
        public List<CommitFileDiffChangeSpanCache> ChangeSpans { get; set; } = new();
    }

    public sealed record CommitFileDiffSyntaxSpanCache
    {
        [Column("start")]
        public int Start { get; set; }
        [Column("length")]
        public int Length { get; set; }
        [Column("scope")]
        public string Scope { get; set; } = string.Empty;
    }

    public sealed record CommitFileDiffChangeSpanCache
    {
        [Column("start")]
        public int Start { get; set; }
        [Column("length")]
        public int Length { get; set; }
        [Column("changetype")]
        public string ChangeType { get; set; } = string.Empty;
    }
}
