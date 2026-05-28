using Tapper;

namespace ExpressThat.LovelyGit.Services.Hubs.Commands
{
    [TranspilationSource]
    public enum CommsHubCommandType
    {
        KnownGitRepositorys,
        CommitGraph,
        Settings
    }

    [TranspilationSource]
    public enum CommsHubSubCommandType
    {
        Get,
        Create,
        Update,
        Delete
    }
}
