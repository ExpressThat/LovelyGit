using ExpressThat.LovelyGit.Services;
using ExpressThat.LovelyGit.Services.Git.CommitGraph;
using Microsoft.Extensions.DependencyInjection;

namespace LovelyGit.Tests.Services;

public sealed class DependencyGraphTests
{
    [Fact]
    public void LovelyGitServices_HaveAValidDependencyGraph()
    {
        var services = new ServiceCollection();
        services.AddSingleton(new CommitGraphBackgroundWorkerOptions(false, false, false));
        services.AddLovelyGitServices();

        using var provider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true,
        });
    }
}
