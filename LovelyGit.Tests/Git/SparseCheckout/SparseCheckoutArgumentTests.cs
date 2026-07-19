using ExpressThat.LovelyGit.Services.Git.SparseCheckout;

namespace LovelyGit.Tests.Git.SparseCheckout;

public sealed class SparseCheckoutArgumentTests
{
    [Fact]
    public void SetUsesStandardInputForPatterns()
    {
        var arguments = GitSparseCheckoutCommandService.BuildArguments(
            SparseCheckoutAction.Set,
            false);

        Assert.Equal("--stdin", arguments[^1]);
    }

    [Fact]
    public void DisableUsesBoundedParallelismWhenRestoringAllFiles()
    {
        var arguments = GitSparseCheckoutCommandService.BuildArguments(
            SparseCheckoutAction.Disable,
            false);

        Assert.Equal(
            ["-c", "checkout.workers=6", "sparse-checkout", "disable"],
            arguments);
    }
}
