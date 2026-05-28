using Tapper;

namespace ExpressThat.LovelyGit.Services.Hubs.Commands
{
    [TranspilationSource]
    public class CommandResponseBase
    {
        public string? CommandUniqueId { get; set; }
        public CommsHubCommandType CommandType { get; set; }
        public CommsHubSubCommandType? SubCommandType { get; set; }
        public bool IsSuccess { get; set; }
        public string? ErrorMessage { get; set; }
    }


    [TranspilationSource]
    public class CommandResponse<T> : CommandResponseBase
    {
        public T? Result { get; set; }
    }
}
