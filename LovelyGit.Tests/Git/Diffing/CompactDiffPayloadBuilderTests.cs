using System.IO.Compression;
using System.Text.Json;
using ExpressThat.LovelyGit.Services.Git.CommitGraph.Models;
using ExpressThat.LovelyGit.Services.Git.Diffing;

namespace LovelyGit.Tests.Git.Diffing;

public sealed class CompactDiffPayloadBuilderTests
{
    [Fact]
    public void CompactIfUseful_PreservesRenderingSpansInVersionedPayload()
    {
        var response = CreateLargeResponse();
        response.Lines[0].OldSyntaxSpans = [new() { Start = 1, Length = 2, Scope = "keyword" }];
        response.Lines[0].NewChangeSpans = [new() { Start = 3, Length = 4, ChangeType = "Inserted" }];

        var compact = CompactDiffPayloadBuilder.CompactIfUseful(response);

        Assert.Equal("tuple-v2:gzip-base64:utf-8", compact.CompactLineSchema);
        Assert.Equal(750, compact.CompactLineCount);
        Assert.Empty(compact.Lines);
        using var document = Decode(compact.CompactLinesGzipBase64);
        var first = document.RootElement[0];
        Assert.Equal("keyword", first[6][0][2].GetString());
        Assert.Equal("Inserted", first[10][0][2].GetString());
    }

    [Fact]
    public void CompactIfUseful_LeavesSmallResponsesUnchanged()
    {
        var response = new CommitFileDiffResponse
        {
            Lines = [new() { Text = "small", ChangeType = "Unchanged" }],
        };

        var unchanged = CompactDiffPayloadBuilder.CompactIfUseful(response);

        Assert.Single(unchanged.Lines);
        Assert.Null(unchanged.CompactLinesGzipBase64);
    }

    private static CommitFileDiffResponse CreateLargeResponse() => new()
    {
        Lines = Enumerable.Range(0, 750)
            .Select(index => new CommitFileDiffLine
            {
                OldLineNumber = index + 1,
                NewLineNumber = index + 1,
                Text = new string('x', 100),
                ChangeType = "Unchanged",
            })
            .ToList(),
    };

    private static JsonDocument Decode(string payload)
    {
        var compressed = Convert.FromBase64String(payload);
        using var input = new MemoryStream(compressed);
        using var gzip = new GZipStream(input, CompressionMode.Decompress);
        return JsonDocument.Parse(gzip);
    }
}
