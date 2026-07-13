using System.Text;
using ExpressThat.LovelyGit.Services.Git.WorkingTree;
using ExpressThat.LovelyGit.Services.Git.WorkingTree.Models;

namespace LovelyGit.Tests.Git.WorkingTree;

public sealed class ConflictResolutionCachedVariantTests
{
    [Fact]
    public void BuildCachedVariant_ReusesCompactTextAndRetainedSources()
    {
        var baseText = Lines("base");
        var oursText = Lines("ours");
        var theirsText = Lines("theirs");
        var resultText = Lines("result");
        var sibling = new ConflictResolutionResponse
        {
            Path = "large.txt",
            Base = Version(baseText),
            Ours = Version(oursText),
            Theirs = Version(theirsText),
            Result = Version(resultText),
        };
        var retained = ConflictTextPayloadBuilder.RetainSources(sibling);
        ConflictTextPayloadBuilder.Compact(sibling);

        var variant = ConflictResolutionService.BuildCachedVariant(
            sibling, retained, ignoreWhitespace: true);

        Assert.Same(sibling.CompactTextBundleGzipBase64, variant.CompactTextBundleGzipBase64);
        Assert.Null(variant.Base.Text);
        Assert.Null(variant.Ours.Text);
        Assert.Null(variant.Theirs.Text);
        Assert.Null(variant.Result.Text);
        Assert.NotNull(variant.CurrentComparison);
        Assert.NotNull(variant.IncomingComparison);
        var expanded = ConflictTextPayloadBuilder.Expand(variant);
        Assert.Equal(baseText, expanded.Base);
        Assert.Equal(oursText, expanded.Ours);
        Assert.Equal(theirsText, expanded.Theirs);
        Assert.Equal(resultText, expanded.Result);
    }

    private static string Lines(string prefix)
    {
        var builder = new StringBuilder();
        for (var index = 0; index < 5_000; index++)
        {
            builder.Append(prefix).Append(' ').Append(index).Append('\n');
        }
        return builder.ToString();
    }

    private static ConflictFileVersion Version(string text) => new()
    {
        Exists = true,
        SizeBytes = Encoding.UTF8.GetByteCount(text),
        Text = text,
    };
}
