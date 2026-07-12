using System.IO.Compression;
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

        Assert.Equal(text, Decode(response.Base));
        Assert.Equal(text + " ours", Decode(response.Ours));
        Assert.Equal(text + " theirs", Decode(response.Theirs));
        Assert.Equal(text + " result", Decode(response.Result));
        Assert.All(
            new[] { response.Base, response.Ours, response.Theirs, response.Result },
            version =>
            {
                Assert.Null(version.Text);
                Assert.Equal("gzip-base64:utf-8", version.TextEncoding);
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
    }

    private static ConflictFileVersion Version(string text) => new()
    {
        Exists = true,
        Text = text,
        SizeBytes = Encoding.UTF8.GetByteCount(text),
    };

    private static string Decode(ConflictFileVersion version)
    {
        var bytes = Convert.FromBase64String(version.TextGzipBase64!);
        using var input = new MemoryStream(bytes);
        using var gzip = new GZipStream(input, CompressionMode.Decompress);
        using var reader = new StreamReader(gzip, Encoding.UTF8);
        return reader.ReadToEnd();
    }
}
