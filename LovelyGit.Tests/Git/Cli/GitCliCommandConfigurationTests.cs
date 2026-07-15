using ExpressThat.LovelyGit.Services.Git.Cli;

namespace LovelyGit.Tests.Git.Cli;

public sealed class GitCliCommandConfigurationTests
{
    [Fact]
    public void CreateCommand_BoundsParallelCheckoutForLargeUpdates()
    {
        var command = new GitCliService().CreateCommand(["status", "--short"]);

        Assert.StartsWith(
            "-c checkout.workers=4 -c checkout.thresholdForParallelism=100 ",
            command.Arguments,
            StringComparison.Ordinal);
        Assert.EndsWith("status --short", command.Arguments, StringComparison.Ordinal);
    }
}
