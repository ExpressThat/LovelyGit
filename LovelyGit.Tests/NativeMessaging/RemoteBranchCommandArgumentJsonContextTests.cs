using System.Text.Json;
using ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.Branches;

namespace LovelyGit.Tests.NativeMessaging;

public sealed class RemoteBranchCommandArgumentJsonContextTests
{
    private static readonly Guid RepositoryId =
        Guid.Parse("bd3d4c1a-5061-453c-abef-70a2aafa6050");

    [Fact]
    public void CheckoutContext_DeserializesCamelCaseArguments()
    {
        var arguments = JsonSerializer.Deserialize(
            $$"""
            {
              "repositoryId": "{{RepositoryId}}",
              "remoteBranchName": "origin/feature/demo",
              "localBranchName": "feature/demo"
            }
            """,
            BranchesJsonSerializerContext.Default.CheckoutRemoteBranchCommandArguments);

        Assert.Equal(RepositoryId, arguments?.RepositoryId);
        Assert.Equal("origin/feature/demo", arguments?.RemoteBranchName);
        Assert.Equal("feature/demo", arguments?.LocalBranchName);
    }

    [Fact]
    public void DeleteContext_DeserializesCamelCaseArguments()
    {
        var arguments = JsonSerializer.Deserialize(
            $$"""
            {
              "repositoryId": "{{RepositoryId}}",
              "remoteBranchName": "origin/feature/demo"
            }
            """,
            BranchesJsonSerializerContext.Default.DeleteRemoteBranchCommandArguments);

        Assert.Equal(RepositoryId, arguments?.RepositoryId);
        Assert.Equal("origin/feature/demo", arguments?.RemoteBranchName);
    }
}
