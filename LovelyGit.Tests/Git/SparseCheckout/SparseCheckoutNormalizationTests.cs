using ExpressThat.LovelyGit.Services.Git.SparseCheckout;
using Xunit.Abstractions;

namespace LovelyGit.Tests.Git.SparseCheckout;

public sealed class SparseCheckoutNormalizationTests(ITestOutputHelper output)
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

    [Theory]
    [InlineData("../src")]
    [InlineData("src/../docs")]
    [InlineData("src/.git/hooks")]
    public void NormalizePatternText_RejectsForbiddenConeSegments(string pattern)
    {
        Assert.Throws<ArgumentException>(() =>
            GitSparseCheckoutCommandService.NormalizePatternText(pattern, coneMode: true));
    }

    [Theory]
    [InlineData("src/.../docs")]
    [InlineData("src/.github/workflows")]
    [InlineData("src//feature")]
    public void NormalizePatternText_PreservesAllowedConeSegments(string pattern)
    {
        Assert.Equal(
            $"{pattern}\n",
            GitSparseCheckoutCommandService.NormalizePatternText(pattern, coneMode: true));
    }

    [Fact]
    public void NormalizePatternText_PreparesMaximumRealisticDraftWithinBudget()
    {
        var patterns = string.Join(
            '\n',
            Enumerable.Range(0, 100_000).Select(index => $"modules/path-{index}"));
        _ = GitSparseCheckoutCommandService.NormalizePatternText(patterns, coneMode: true);
        var allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        var result = GitSparseCheckoutCommandService.NormalizePatternText(patterns, coneMode: true);

        stopwatch.Stop();
        var allocated = GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;
        output.WriteLine($"Elapsed: {stopwatch.Elapsed.TotalMilliseconds:F2} ms");
        output.WriteLine($"Allocated: {allocated / 1024d / 1024d:F2} MiB");
        Assert.StartsWith("modules/path-0\n", result, StringComparison.Ordinal);
        Assert.EndsWith("modules/path-99999\n", result, StringComparison.Ordinal);
        Assert.True(stopwatch.Elapsed < TimeSpan.FromMilliseconds(100));
        Assert.True(allocated < 20 * 1024 * 1024);
    }
}
