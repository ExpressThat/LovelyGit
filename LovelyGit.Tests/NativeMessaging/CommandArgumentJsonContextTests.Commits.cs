using ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.CherryPick;
using ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.Revert;

namespace LovelyGit.Tests.NativeMessaging;

public sealed partial class CommandArgumentJsonContextTests
{
    private static string CreateCommitListJson() => $$"""
        {
          "repositoryId": "{{RepositoryId}}",
          "commitHashes": ["abc123", "def456"]
        }
        """;

    private static void AssertCommit(CherryPickCommitCommandArguments? arguments)
    {
        Assert.NotNull(arguments);
        Assert.Equal(RepositoryId, arguments.RepositoryId);
        Assert.Equal(["abc123", "def456"], arguments.CommitHashes);
    }

    private static void AssertCommit(RevertCommitCommandArguments? arguments)
    {
        Assert.NotNull(arguments);
        Assert.Equal(RepositoryId, arguments.RepositoryId);
        Assert.Equal(["abc123", "def456"], arguments.CommitHashes);
    }
}
