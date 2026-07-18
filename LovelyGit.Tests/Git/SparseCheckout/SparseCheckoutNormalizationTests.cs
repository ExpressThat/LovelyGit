using ExpressThat.LovelyGit.Services.Git.SparseCheckout;

namespace LovelyGit.Tests.Git.SparseCheckout;

public sealed class SparseCheckoutNormalizationTests
{
    [Fact]
    public void NormalizePatternText_TrimsNormalizesAndDeduplicates()
    {
        var result = GitSparseCheckoutCommandService.NormalizePatternText(
            " src\\feature \r\nsrc/feature\n\ndocs/**",
            coneMode: false);

        Assert.Equal("src/feature\ndocs/**\n", result);
    }

    [Fact]
    public void NormalizePatternText_RejectsMoreThanTheBoundedMaximum()
    {
        var patterns = string.Join('\n', Enumerable.Range(0, 250_001));

        var exception = Assert.Throws<ArgumentException>(() =>
            GitSparseCheckoutCommandService.NormalizePatternText(patterns, coneMode: false));

        Assert.Contains("250,000", exception.Message);
    }
}
