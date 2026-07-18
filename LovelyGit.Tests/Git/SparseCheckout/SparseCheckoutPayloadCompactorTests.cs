using System.IO.Compression;
using System.Text;
using ExpressThat.LovelyGit.Services.Git.SparseCheckout;

namespace LovelyGit.Tests.Git.SparseCheckout;

public sealed class SparseCheckoutPayloadCompactorTests
{
    [Fact]
    public void CompactIfUseful_LeavesSmallSpecificationsReadable()
    {
        var state = new SparseCheckoutState { PatternCount = 1, PatternText = "src" };

        var compact = SparseCheckoutPayloadCompactor.CompactIfUseful(state);

        Assert.Same(state, compact);
        Assert.Empty(compact.PatternTextGzipBase64);
    }

    [Fact]
    public void CompactIfUseful_RoundTripsLargeSpecifications()
    {
        var text = string.Join('\n', Enumerable.Range(0, 100_000).Select(index => $"path-{index}"));

        var compact = SparseCheckoutPayloadCompactor.CompactIfUseful(
            new SparseCheckoutState { PatternCount = 100_000, PatternText = text });

        Assert.Empty(compact.PatternText);
        Assert.True(compact.PatternTextGzipBase64.Length < text.Length / 2);
        Assert.Equal(text, Decompress(compact.PatternTextGzipBase64));
    }

    [Fact]
    public void ExpandRequest_RoundTripsCompactSpecifications()
    {
        var text = string.Join('\n', Enumerable.Range(0, 100_000).Select(index => $"path-{index}"));
        var compact = SparseCheckoutPayloadCompactor.CompactIfUseful(
            new SparseCheckoutState { PatternText = text });

        var result = SparseCheckoutPayloadCompactor.ExpandRequest(
            compact.PatternText,
            compact.PatternTextGzipBase64);

        Assert.Equal(text, result);
    }

    [Fact]
    public void ExpandRequest_RejectsAmbiguousOrMalformedPayloads()
    {
        Assert.Throws<ArgumentException>(() =>
            SparseCheckoutPayloadCompactor.ExpandRequest("src", "compressed"));
        Assert.Throws<FormatException>(() =>
            SparseCheckoutPayloadCompactor.ExpandRequest(string.Empty, "not base64"));
    }

    private static string Decompress(string value)
    {
        using var input = new MemoryStream(Convert.FromBase64String(value));
        using var gzip = new GZipStream(input, CompressionMode.Decompress);
        using var reader = new StreamReader(gzip, Encoding.UTF8);
        return reader.ReadToEnd();
    }
}
