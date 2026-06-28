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
}
