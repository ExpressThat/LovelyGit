using System.Text.Json;
using ExpressThat.LovelyGit.Services.Git.Lfs;
using ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.Lfs;

namespace LovelyGit.Tests.NativeMessaging.Lfs;

public sealed class LfsContractTests
{
    [Fact]
    public void ManageArguments_DeserializeStringActionAndPattern()
    {
        var repositoryId = Guid.NewGuid();
        var json = $$"""
            {"repositoryId":"{{repositoryId}}","action":"Track","pattern":"Video/**"}
            """;

        var arguments = JsonSerializer.Deserialize(
            json,
            LfsJsonSerializerContext.Default.ManageGitLfsCommandArguments);

        Assert.NotNull(arguments);
        Assert.Equal(repositoryId, arguments.RepositoryId);
        Assert.Equal(GitLfsAction.Track, arguments.Action);
        Assert.Equal("Video/**", arguments.Pattern);
    }
}
