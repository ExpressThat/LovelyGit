using System.IO.Compression;
using System.Text;
using ExpressThat.LovelyGit.Services.Git.WorkingTree;

namespace LovelyGit.Tests.Git.WorkingTree;

public sealed class ConflictResultPayloadTests
{
    [Fact]
    public void Expand_PreservesCompressedUtf8Exactly()
    {
        const string expected = "first\r\nemoji 🚀\nlast";

        Assert.Equal(expected, ConflictResultPayload.Expand(null, Compress(expected)));
    }

    [Fact]
    public void Expand_RejectsAmbiguousMalformedAndOversizedPayloads()
    {
        Assert.Throws<ArgumentException>(() => ConflictResultPayload.Expand("plain", Compress("compressed")));
        Assert.Throws<FormatException>(() => ConflictResultPayload.Expand(null, "not-base64"));
        var oversized = new string('x', ConflictResultPayload.MaximumTextBytes + 1);
        Assert.Throws<ArgumentException>(() => ConflictResultPayload.Expand(null, Compress(oversized)));
    }

    private static string Compress(string value)
    {
        using var output = new MemoryStream();
        using (var gzip = new GZipStream(output, CompressionLevel.Optimal, leaveOpen: true))
        {
            gzip.Write(Encoding.UTF8.GetBytes(value));
        }
        return Convert.ToBase64String(output.GetBuffer(), 0, checked((int)output.Length));
    }
}
