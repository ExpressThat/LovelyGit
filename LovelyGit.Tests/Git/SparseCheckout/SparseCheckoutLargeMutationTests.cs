using ExpressThat.LovelyGit.Services.Git.Cli;
using ExpressThat.LovelyGit.Services.Git.SparseCheckout;

namespace LovelyGit.Tests.Git.SparseCheckout;

public sealed class SparseCheckoutLargeMutationTests
{
    [Fact]
    public async Task Set_RoundTripsMoreThanTheFormerArgumentLimitThroughStandardInput()
    {
        using var repository = SparseRepository.Create();
        var patternText = string.Join(
            '\n',
            Enumerable.Range(0, 600).Select(index => $"modules/path-{index}"));
        var service = new GitSparseCheckoutCommandService(
            new GitCliService(),
            new NativeSparseCheckoutReader());

        var state = await service.ExecuteAsync(
            repository.Path,
            SparseCheckoutAction.Set,
            coneMode: true,
            patternText,
            CancellationToken.None);

        Assert.True(state.Enabled);
        Assert.True(state.ConeMode);
        Assert.Equal(600, state.PatternCount);
        Assert.Contains("modules/path-599", state.PatternText.Split('\n'));
    }
}
