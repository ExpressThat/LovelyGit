using System.IO.Compression;
using ExpressThat.LovelyGit.Services.Git.WorkingTree;

namespace LovelyGit.Tests.Git.WorkingTree;

public sealed class ConflictTextBundleCodecTests
{
    [Fact]
    public void RoundTrip_PreservesUnicodeEmptySourcesAndFinalNewlines()
    {
        var bundle = ConflictTextBundleCodec.Compress(
            "base\n🙂\n",
            string.Empty,
            "theirs\r\nwithout final newline",
            "result\n");

        var texts = ConflictTextBundleCodec.Expand(bundle);

        Assert.Equal("base\n🙂\n", texts.Base);
        Assert.Equal(string.Empty, texts.Ours);
        Assert.Equal("theirs\r\nwithout final newline", texts.Theirs);
        Assert.Equal("result\n", texts.Result);
    }

    [Theory]
    [InlineData(new byte[] { 0x01 }, "truncated")]
    [InlineData(new byte[] { 0x00, 0x01, 0x00 }, "trailing data")]
    [InlineData(new byte[] { 0x80, 0x80, 0x80, 0x80, 0x80 }, "invalid integer")]
    [InlineData(new byte[] { 0xff, 0xff, 0xff, 0xff, 0x10 }, "invalid integer")]
    [InlineData(new byte[] { 0xff, 0xff, 0xff, 0xff, 0x0f }, "row count is too large")]
    [InlineData(new byte[] { 0x00, 0xff, 0xff, 0xff, 0xff, 0x0f }, "text is too large")]
    public void Expand_RejectsMalformedPayloads(byte[] raw, string expected)
    {
        var error = Assert.Throws<InvalidDataException>(() =>
            ConflictTextBundleCodec.Expand(CompressRaw(raw)));

        Assert.Contains(expected, error.Message, StringComparison.OrdinalIgnoreCase);
    }

    private static string CompressRaw(byte[] raw)
    {
        using var output = new MemoryStream();
        using (var gzip = new GZipStream(output, CompressionLevel.Fastest, leaveOpen: true))
        {
            gzip.Write(raw);
        }
        return Convert.ToBase64String(output.GetBuffer(), 0, checked((int)output.Length));
    }
}
