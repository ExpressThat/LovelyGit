namespace ExpressThat.LovelyGit.Services.Hubs.Commands
{
    public class CommandResponse
    {
        public string? CommandUniqueId { get; set; }
        public CommsHubCommandType CommandType { get; set; }
        public CommsHubSubCommandType? SubCommandType { get; set; }
        public bool IsSuccess { get; set; }
        public string? ErrorMessage { get; set; }
    }

    public class CommandResponse<T> : CommandResponse
    {
        public T? Result { get; set; }
    }
}
