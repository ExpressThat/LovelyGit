using System.Text.Json;
using ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.KnownRepository;

namespace LovelyGit.Tests.NativeMessaging;

public sealed class KnownRepositoryJsonContextTests
{
    [Fact]
    public void Context_DeserializesRevealRepositoryArguments()
    {
        var repositoryId = Guid.Parse("bd3d4c1a-5061-453c-abef-70a2aafa6050");

        var arguments = JsonSerializer.Deserialize(
            $$"""
            {
              "knownRepositoryId": "{{repositoryId}}"
            }
            """,
            KnownRepositoriesJsonSerializerContext.Default.RevealKnownGitRepositoryCommandArguments);

        Assert.Equal(repositoryId, arguments?.KnownRepositoryId);
    }

    [Fact]
    public void Context_DeserializesInitializeRepositoryArguments()
    {
        var arguments = JsonSerializer.Deserialize(
            """
            {
              "parentPath": "C:\\projects",
              "directoryName": "lovely-project",
              "initialBranchName": "trunk"
            }
            """,
            KnownRepositoriesJsonSerializerContext.Default.InitializeRepositoryCommandArguments);

        Assert.NotNull(arguments);
        Assert.Equal("C:\\projects", arguments.ParentPath);
        Assert.Equal("lovely-project", arguments.DirectoryName);
        Assert.Equal("trunk", arguments.InitialBranchName);
    }
}
