using System.Text;
using System.Text.Json;
using ExpressThat.LovelyGit.Services.Git.CommitGraph.Models;
using ExpressThat.LovelyGit.Services.Git.WorkingTree;

namespace LovelyGit.Tests.Git.WorkingTree;

public sealed class PreparedConflictComparisonTests
{
    [Fact]
    public void PreparedResponse_MatchesRegularSideBySideResponse()
    {
        const string oldText = "before\nold value\nafter\n";
        const string newText = "before\nnew value\nafter\n";
        var expected = WorkingTreeChangeService.BuildDiffResponse(
            "CONFLICT",
            "sample.cs",
            "Unmerged",
            CommitDiffViewMode.SideBySide,
            ignoreWhitespace: false,
            Encoding.UTF8.GetBytes(oldText),
            Encoding.UTF8.GetBytes(newText),
            compact: false);

        var actual = WorkingTreeChangeService.BuildPreparedSideBySideResponse(
            "CONFLICT",
            "sample.cs",
            "Unmerged",
            oldText,
            newText,
            ConflictHunkBuilder.BuildModel(oldText, newText));

        Assert.Equal(expected.HasDifferences, actual.HasDifferences);
        Assert.Equal(JsonSerializer.Serialize(expected.Lines), JsonSerializer.Serialize(actual.Lines));
    }

    [Fact]
    public void PreparedWhitespaceResponse_PreservesIgnoredWhitespaceSemantics()
    {
        const string oldText = "value = 1;\n";
        const string newText = "value  =  1;\n";
        var model = ConflictHunkBuilder.BuildModel(oldText, newText, ignoreWhitespace: true);

        var actual = WorkingTreeChangeService.BuildPreparedSideBySideResponse(
            "CONFLICT",
            "sample.cs",
            "Unmerged",
            oldText,
            newText,
            model);

        Assert.False(actual.HasDifferences);
        Assert.All(actual.Lines, line => Assert.Equal("Unchanged", line.ChangeType));
    }
}
