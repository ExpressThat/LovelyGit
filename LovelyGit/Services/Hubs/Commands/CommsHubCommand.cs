using Tapper;

namespace ExpressThat.LovelyGit.Services.Hubs.Commands
{
    [TranspilationSource]
    public record CommsHubCommand
    {
        public string? CommandUniqueId { get; set; }
        public CommsHubCommandType CommandType { get; set; }
        public CommsHubSubCommandType? SubCommandType { get; set; }
        public string? Key { get; set; }
        public Dictionary<string, string>? Arguments { get; set; } = null!;
    }
}
