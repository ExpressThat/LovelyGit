namespace ExpressThat.LovelyGit.Services.Hubs.Commands
{

    public interface ICommandResponder
    {
        bool CanRespondTo(CommsHubCommand command);

        Task<CommandResponse> Resolve(CommsHubCommand command);
    }
}
