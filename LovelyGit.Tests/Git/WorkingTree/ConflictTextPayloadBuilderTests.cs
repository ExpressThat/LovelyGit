using System.Text;
using ExpressThat.LovelyGit.Services.Git.WorkingTree;
using ExpressThat.LovelyGit.Services.Git.WorkingTree.Models;

namespace LovelyGit.Tests.Git.WorkingTree;

public sealed class ConflictTextPayloadBuilderTests
{
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
        Assert.Equal("interleaved-lines-v2:gzip-base64:utf-8", response.CompactTextSchema);
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

    private static ConflictFileVersion Version(string text) => new()
    {
        Exists = true,
        Text = text,
        SizeBytes = Encoding.UTF8.GetByteCount(text),
    };

    private static string?[] Expand(ConflictResolutionResponse response)
    {
        var texts = ConflictTextPayloadBuilder.Expand(response);
        return [texts.Base, texts.Ours, texts.Theirs, texts.Result];
    }
}
