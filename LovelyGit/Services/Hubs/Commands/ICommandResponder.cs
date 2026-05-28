namespace ExpressThat.LovelyGit.Services.Hubs.Commands
{

    public interface ICommandResponder
    {
        bool CanRespondTo(CommsHubCommand command);

        Task<CommandResponseBase> Resolve(CommsHubCommand command);
    }
}
