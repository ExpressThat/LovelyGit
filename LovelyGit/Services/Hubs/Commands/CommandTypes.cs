namespace ExpressThat.LovelyGit.Services.Hubs.Commands
{
    public enum CommsHubCommandType
    {
        KnownGitRepositorys,
        Settings
    }

    public enum CommsHubSubCommandType
    {
        Get,
        Create,
        Update,
        Delete
    }
}
