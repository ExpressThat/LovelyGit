using ExpressThat.LovelyGit.Services.TypeGeneration;

namespace ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.CommitGraph
{
    [TypeSharp]
    public record CommitGraphCommandArguments
    {
        public Guid KnownRepositoryId { get; set; }
        public string? Cursor { get; set; }
        public int Limit { get; set; }
    }

    [TypeSharp]
    public record GetRepositoryRefsCommandArguments
    {
        public Guid KnownRepositoryId { get; set; }
    }

    [TypeSharp]
    public record GetReflogCommandArguments
    {
        public Guid KnownRepositoryId { get; set; }
        public string? BranchName { get; set; }
        public int Limit { get; set; } = 200;
    }

    [TypeSharp]
    public record SearchCommitsCommandArguments
    {
        public Guid KnownRepositoryId { get; set; }
        public string Query { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
        public string Scope { get; set; } = string.Empty;
        public long? AfterUnixSeconds { get; set; }
        public long? BeforeUnixSeconds { get; set; }
        public int Limit { get; set; } = 50;
        public bool Deep { get; set; }
    }

    [TypeSharp]
    public record CancelCommitSearchCommandArguments
    {
        public Guid KnownRepositoryId { get; set; }
    }

    [TypeSharp]
    public record GetFileHistoryCommandArguments
    {
        public Guid KnownRepositoryId { get; set; }
        public string Path { get; set; } = string.Empty;
        public string? StartCommitHash { get; set; }
        public int Limit { get; set; } = 100;
        public bool Deep { get; set; }
    }

    [TypeSharp]
    public record CancelFileHistoryCommandArguments
    {
        public Guid KnownRepositoryId { get; set; }
    }

    [TypeSharp]
    public record GetFileBlameCommandArguments
    {
        public Guid KnownRepositoryId { get; set; }
        public string Path { get; set; } = string.Empty;
        public string? StartCommitHash { get; set; }
        public bool Deep { get; set; }
    }

    [TypeSharp]
    public record CancelFileBlameCommandArguments
    {
        public Guid KnownRepositoryId { get; set; }
    }
}
