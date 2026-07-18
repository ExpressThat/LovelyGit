using System.Text;
using ExpressThat.LovelyGit.Services.Git.CommitGraph.Models;
using ExpressThat.LovelyGit.Services.Git.WorkingTree;

namespace LovelyGit.Tests.Git.WorkingTree;

public sealed class LineEndingMetadataBuilderTests
{
    [Fact]
    public void Analyze_UsesOneDefaultForUniformCrLfWithoutPerLineObjects()
    {
        var profile = Analyze("one\r\ntwo\r\nthree\r\n");

        Assert.Equal("\r\n", profile.Default);
        Assert.Empty(profile.Overrides);
    }

    [Fact]
    public void Analyze_UsesSparseOverridesForMixedAndMissingEndings()
    {
        var profile = Analyze("one\r\ntwo\nthree");

        Assert.Equal("\r\n", profile.Default);
        Assert.Collection(
            profile.Overrides,
            item => Assert.Equal((2 * 4) + 1, item),
            item => Assert.Equal((3 * 4) + 3, item));
    }

    [Theory]
    [InlineData("", "\n")]
    [InlineData("one", "")]
    [InlineData("one\n", "\n")]
    [InlineData("one\r", "\r")]
    public void Analyze_RepresentsBoundaryCases(string text, string expected)
    {
        var profile = Analyze(text);

        Assert.Equal(expected, profile.Default);
        Assert.Empty(profile.Overrides);
    }

    [Fact]
    public void WorkingTreeDiff_CarriesProfilesThroughCompactPayloadPreparation()
    {
        var oldBytes = Encoding.UTF8.GetBytes(string.Concat(
            Enumerable.Repeat("old\r\n", 800)));
        var newBytes = Encoding.UTF8.GetBytes(string.Concat(
            Enumerable.Repeat("new\r\n", 799)) + "last");

        var response = WorkingTreeChangeService.BuildDiffResponse(
            "HEAD", "large.txt", "Modified", CommitDiffViewMode.SideBySide,
            ignoreWhitespace: false, oldBytes, newBytes);

        Assert.NotEmpty(response.CompactLinesGzipBase64);
        Assert.Equal("\r\n", response.OldLineEnding);
        Assert.Equal("\r\n", response.NewLineEnding);
        Assert.Empty(response.OldLineEndingOverrides);
        Assert.Equal([(800 * 4) + 3], response.NewLineEndingOverrides);
    }

    private static LineEndingProfile Analyze(string text) =>
        LineEndingMetadataBuilder.Analyze(Encoding.UTF8.GetBytes(text));
}
