using Tapper;

namespace ExpressThat.LovelyGit.Services.Hubs.CommandResolvers.CommitGraph
{
    [TranspilationSource]
    public record CommitGraphCommandArguments
    {
        public Guid KnownRepositoryId { get; set; }
        public string? Cursor { get; set; }
        public int Limit { get; set; }
    }
}
