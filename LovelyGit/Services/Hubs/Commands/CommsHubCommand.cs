using Tapper;

namespace ExpressThat.LovelyGit.Services.Hubs.Commands
{
    [TranspilationSource]
    public record CommsHubCommand<TArguments>
    {
        public string? CommandUniqueId { get; set; }
        public CommsHubCommandType CommandType { get; set; }
        public CommsHubSubCommandType? SubCommandType { get; set; }
        public string? Key { get; set; }
        public TArguments? Arguments { get; set; }
    }
}
