using ExpressThat.LovelyGit.Services.Git.Patches;

namespace LovelyGit.Tests.Git.Patches;

public sealed class PatchApplyErrorCollectorTests
{
    [Fact]
    public void FormatFailure_WithoutDiagnostics_UsesFallback()
    {
        var collector = new PatchApplyErrorCollector();

        Assert.Equal(
            "Git could not apply this patch to the current repository state.",
            collector.FormatFailure());
    }

    [Fact]
    public void FormatFailure_RetainsOnlyFirstTwoActionableLines()
    {
        var collector = new PatchApplyErrorCollector();
        collector.Add("error: first path");
        collector.Add("error: first path does not apply");
        collector.Add("error: second path");

        Assert.Equal(
            "error: first path\nerror: first path does not apply\n" +
            "Additional patch failures were omitted.",
            collector.FormatFailure());
    }

    [Fact]
    public void FormatFailure_BoundsAnIndividualDiagnosticLine()
    {
        var collector = new PatchApplyErrorCollector();
        collector.Add(new string('x', 1_000));

        var message = collector.FormatFailure();

        Assert.Equal(512, message.Length);
        Assert.EndsWith("...", message, StringComparison.Ordinal);
    }
}
