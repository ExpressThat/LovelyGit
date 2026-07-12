using System.IO.Compression;
using System.Text.Json;
using ExpressThat.LovelyGit.Services.Git.CommitGraph.Models;
using ExpressThat.LovelyGit.Services.Git.WorkingTree;

namespace LovelyGit.Tests.Git.WorkingTree;

public sealed class ConflictComparisonPayloadBuilderTests
{
    [Fact]
    public void Compact_ReferencesSourceTextWhilePreservingRenderingMetadata()
    {
        var response = LargeResponse();
        response.Lines[0].OldSyntaxSpans = [new() { Start = 1, Length = 2, Scope = "keyword" }];
        response.Lines[0].NewChangeSpans = [new() { Start = 3, Length = 4, ChangeType = "Inserted" }];

        ConflictComparisonPayloadBuilder.Compact(response);

        Assert.Equal("tuple-v4-delta-refs:gzip-base64:utf-8", response.CompactLineSchema);
        Assert.Equal(750, response.CompactLineCount);
        Assert.True(response.CompactLinesGzipBase64.Length < 2_000);
        Assert.Empty(response.Lines);
        using var document = Decode(response.CompactLinesGzipBase64);
        var first = document.RootElement[0];
        Assert.Equal(1, first[0].GetInt32());
        Assert.Equal(1, first[1].GetInt32());
        Assert.Equal(1, first[2].GetInt32());
        Assert.Equal("keyword", first[3][0][2].GetString());
        Assert.Equal("Inserted", first[7][0][2].GetString());
        var unchanged = document.RootElement[1];
        Assert.Equal(1, unchanged[0].GetInt32());
        Assert.Equal(3, unchanged.GetArrayLength());
    }

    [Fact]
    public void Compact_LeavesSmallComparisonsExpanded()
    {
        var response = new CommitFileDiffResponse { Lines = [Line(1)] };

        ConflictComparisonPayloadBuilder.Compact(response);

        Assert.Single(response.Lines);
        Assert.Null(response.CompactLinesGzipBase64);
    }

    private static CommitFileDiffResponse LargeResponse() => new()
    {
        Lines = Enumerable.Range(1, 750).Select(Line).ToList(),
    };

    private static CommitFileDiffLine Line(int number) => new()
    {
        OldLineNumber = number,
        NewLineNumber = number,
        OldText = $"base line {number}",
        NewText = $"source line {number}",
        ChangeType = "Modified",
    };

    private static JsonDocument Decode(string payload)
    {
        var bytes = Convert.FromBase64String(payload);
        using var input = new MemoryStream(bytes);
        using var gzip = new GZipStream(input, CompressionMode.Decompress);
        return JsonDocument.Parse(gzip);
    }
}
