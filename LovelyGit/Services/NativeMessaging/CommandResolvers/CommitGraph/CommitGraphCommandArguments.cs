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
        public int Limit { get; set; } = 50;
        public bool Deep { get; set; }
    }
}
