namespace ExpressThat.LovelyGit.Services.Hubs.Commands
{
    public enum CommsHubCommandType
    {
        KnownGitRepositorys,
        CommitGraph,
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
