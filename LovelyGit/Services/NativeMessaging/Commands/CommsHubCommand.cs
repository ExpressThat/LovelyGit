using ExpressThat.LovelyGit.Services.NativeMessaging;

namespace ExpressThat.LovelyGit.Services.NativeMessaging.Commands
{
    public record NativeCommand<TArguments>
    {
        public string? CommandUniqueId { get; set; }
        public NativeMessageType CommandType { get; set; }
        public TArguments? Arguments { get; set; }
    }
}
