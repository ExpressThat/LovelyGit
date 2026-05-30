using Tapper;

namespace ExpressThat.LovelyGit.Services.Hubs.CommandResolvers.CommitGraph
{
    [TranspilationSource]
    public record CommitGraphCommandArguments
    {
        public string? Cursor { get; set; }
        public int Limit { get; set; }
    }
}
