using System.IO.Compression;
using System.Text;
using System.Text.Json;
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
            Decode(response));
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
    }

    private static ConflictFileVersion Version(string text) => new()
    {
        Exists = true,
        Text = text,
        SizeBytes = Encoding.UTF8.GetByteCount(text),
    };

    private static string[] Decode(ConflictResolutionResponse response)
    {
        var bytes = Convert.FromBase64String(response.CompactTextBundleGzipBase64!);
        using var input = new MemoryStream(bytes);
        using var gzip = new GZipStream(input, CompressionMode.Decompress);
        using var document = JsonDocument.Parse(gzip);
        var texts = new[] { new StringBuilder(), new StringBuilder(), new StringBuilder() };
        foreach (var row in document.RootElement[0].EnumerateArray())
        {
            for (var index = 0; index < texts.Length; index++)
            {
                if (row[index].ValueKind is not JsonValueKind.Null)
                    texts[index].Append(row[index].GetString());
            }
        }
        return [.. texts.Select(text => text.ToString()), document.RootElement[1].GetString()!];
    }
}
