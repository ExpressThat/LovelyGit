using System.Text;
using ExpressThat.LovelyGit.Services.Git.WorkingTree;
using ExpressThat.LovelyGit.Services.Git.WorkingTree.Models;
using Xunit.Abstractions;

namespace LovelyGit.Tests.Git.WorkingTree;

public sealed class ConflictTextPayloadBuilderTests(ITestOutputHelper output)
{
    [Fact]
    public void Compact_KeepsMaximumMostlySharedConflictInsideBridgeBudget()
    {
        const int lineCount = 100_000;
        var baseText = BuildLines(lineCount, static index => $"base line {index}");
        var oursText = BuildLines(
            lineCount,
            static index => index == 50_000 ? "current changed line 50000" : $"base line {index}");
        var theirsText = BuildLines(
            lineCount,
            static index => index % 1_000 == 0 ? $"target changed line {index}" : $"base line {index}");
        var resultText = theirsText.Replace(
            "target changed line 50000\n",
            "<<<<<<< HEAD\ncurrent changed line 50000\n=======\n" +
            "target changed line 50000\n>>>>>>> comparison-target\n",
            StringComparison.Ordinal);
        var response = new ConflictResolutionResponse
        {
            Base = Version(baseText),
            Ours = Version(oursText),
            Theirs = Version(theirsText),
            Result = Version(resultText),
        };

        ConflictTextPayloadBuilder.Compact(response);

        var bundle = Assert.IsType<string>(response.CompactTextBundleGzipBase64);
        output.WriteLine($"BundleCharacters={bundle.Length:N0}");
        Assert.True(bundle.Length < 800_000, $"Bundle has {bundle.Length:N0} characters.");
        Assert.Equal(new[] { baseText, oursText, theirsText, resultText }, Expand(response));
    }

    [Fact]
    public void Compact_CompressesEveryLargeVersionWithoutChangingItsText()
    {
        var text = string.Join('\n', Enumerable.Range(1, 5_000).Select(index => $"base line {index}"));
        var response = new ConflictResolutionResponse
        {
            Base = Version(text),
            Ours = Version(text + " ours"),
            Theirs = Version(text + " theirs"),
            Result = Version(text + " result"),
        };

        ConflictTextPayloadBuilder.Compact(response);

        Assert.Equal(
            new[] { text, text + " ours", text + " theirs", text + " result" },
            Expand(response));
        Assert.Equal("interleaved-lines-v3:gzip-base64:varint-utf-8", response.CompactTextSchema);
        Assert.All(
            new[] { response.Base, response.Ours, response.Theirs, response.Result },
            version =>
            {
                Assert.Null(version.Text);
                Assert.Null(version.TextEncoding);
                Assert.Null(version.TextGzipBase64);
            });
    }

    [Fact]
    public void Compact_LeavesSmallTextImmediatelyAvailable()
    {
        var response = new ConflictResolutionResponse { Base = Version("small") };

        ConflictTextPayloadBuilder.Compact(response);

        Assert.Equal("small", response.Base.Text);
        Assert.Null(response.Base.TextGzipBase64);
        Assert.Null(response.Base.TextEncoding);
        Assert.Null(response.CompactTextBundleGzipBase64);
        Assert.Equal(new[] { "small", null, null, null }, Expand(response));
    }

    [Fact]
    public void Expand_RejectsUnknownCompactSchema()
    {
        var response = new ConflictResolutionResponse
        {
            CompactTextSchema = "future",
            CompactTextBundleGzipBase64 = Convert.ToBase64String([1]),
        };

        var error = Assert.Throws<InvalidOperationException>(() => ConflictTextPayloadBuilder.Expand(response));

        Assert.Contains("future", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RetainSources_KeepsLargeComparisonTextsButNotTheResult()
    {
        var text = new string('x', 64_000);
        var response = new ConflictResolutionResponse
        {
            Base = Version(text),
            Ours = Version(text),
            Theirs = Version(text),
            Result = Version(text),
        };

        var retained = ConflictTextPayloadBuilder.RetainSources(response);

        Assert.NotNull(retained);
        Assert.Same(text, retained.Value.Base);
        Assert.Same(text, retained.Value.Ours);
        Assert.Same(text, retained.Value.Theirs);
        Assert.Null(retained.Value.Result);
    }

    [Fact]
    public void RetainSources_DoesNotPinOversizedConflicts()
    {
        var response = new ConflictResolutionResponse
        {
            Base = Version(new string('x', (2 * 1024 * 1024) + 1)),
        };

        Assert.Null(ConflictTextPayloadBuilder.RetainSources(response));
    }

    private static ConflictFileVersion Version(string text) => new()
    {
        Exists = true,
        Text = text,
        SizeBytes = Encoding.UTF8.GetByteCount(text),
    };

    private static string BuildLines(int count, Func<int, string> line)
    {
        var text = new StringBuilder(count * 16);
        for (var index = 1; index <= count; index++) text.AppendLine(line(index));
        return text.ToString();
    }

    private static string?[] Expand(ConflictResolutionResponse response)
    {
        var texts = ConflictTextPayloadBuilder.Expand(response);
        return [texts.Base, texts.Ours, texts.Theirs, texts.Result];
    }
}
