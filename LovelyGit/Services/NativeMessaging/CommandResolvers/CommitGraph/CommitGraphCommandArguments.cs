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
}
