using ExpressThat.LovelyGit.Services.Git.Diffing;

namespace LovelyGit.Tests.Git.Diffing;

public sealed class LineDiffEngineTests
{
    public static TheoryData<string, string> RepresentativeChanges => new()
    {
        { string.Empty, "added\n" },
        { "deleted\n", string.Empty },
        { "one\ntwo\nthree\n", "one\nchanged\nthree\n" },
        { "same\nrepeat\nsame\n", "same\ninsert\nrepeat\nsame\n" },
        { "a\r\nb\r\n", "a\r\nc\r\n" },
        { "no final newline", "changed without newline" },
        { "same text\n", "same text" },
    };

    [Theory]
    [MemberData(nameof(RepresentativeChanges))]
    public void Build_EditBlocksReconstructNewLines(string oldText, string newText)
    {
        var model = LineDiffEngine.Build(oldText, newText);

        Assert.Equal(model.NewLines, Apply(model));
        Assert.Equal(!string.Equals(oldText, newText, StringComparison.Ordinal), model.HasDifferences);
    }

    [Fact]
    public void Build_IgnoreWhitespaceTreatsSpacesAndTabsAsEquivalent()
    {
        var model = LineDiffEngine.Build("value = 1\n", " value\t=   1\n", ignoreWhitespace: true);

        Assert.False(model.HasDifferences);
        Assert.Single(model.Rows);
        Assert.False(model.Rows[0].IsChanged);
    }

    [Theory]
    [InlineData("same text", "same text", true)]
    [InlineData(" value\t= 1 ", "value =\t1", true)]
    [InlineData("value\u00A0one", "value one", true)]
    [InlineData("value one", "value two", false)]
    public void WhitespaceComparer_PreservesEqualityAndHashContract(string left, string right, bool equal)
    {
        var comparer = WhitespaceIgnoringLineComparer.Instance;

        Assert.Equal(equal, comparer.Equals(left, right));
        if (equal) Assert.Equal(comparer.GetHashCode(left), comparer.GetHashCode(right));
    }

    [Theory]
    [MemberData(nameof(RepresentativeChanges))]
    public void PreparedText_PreservesStringBuildSemantics(string oldText, string newText)
    {
        var oldPrepared = LineDiffEngine.Prepare(oldText);
        var newPrepared = LineDiffEngine.Prepare(newText);

        var prepared = LineDiffEngine.Build(oldPrepared, newPrepared);
        var direct = LineDiffEngine.Build(oldText, newText);

        Assert.Equal(direct.OldLines, prepared.OldLines);
        Assert.Equal(direct.NewLines, prepared.NewLines);
        Assert.Equal(direct.Blocks, prepared.Blocks);
        Assert.Equal(direct.Rows, prepared.Rows);
    }

    [Fact]
    public void PreparedText_CanBeReusedAcrossComparisonModesWithoutMutation()
    {
        var oldPrepared = LineDiffEngine.Prepare("same\r\nvalue = 1\r\n");
        var newPrepared = LineDiffEngine.Prepare("same\n value\t=  1\n");

        var exact = LineDiffEngine.Build(oldPrepared, newPrepared);
        var ignored = LineDiffEngine.Build(oldPrepared, newPrepared, ignoreWhitespace: true);

        Assert.True(exact.HasDifferences);
        Assert.False(ignored.HasDifferences);
        Assert.Equal(new[] { "same", "value = 1" }, oldPrepared.Lines);
        Assert.Equal(new[] { "same", " value\t=  1" }, newPrepared.Lines);
    }

    public static TheoryData<string, string[]> SplitLineCases => new()
    {
        { string.Empty, [] },
        { "one", ["one"] },
        { "one\n", ["one"] },
        { "one\r\n", ["one"] },
        { "one\rtwo", ["one", "two"] },
        { "\n", [string.Empty] },
        { "one\n\n", ["one", string.Empty] },
        { "one\r\ntwo\rthree\n", ["one", "two", "three"] },
    };

    [Theory]
    [MemberData(nameof(SplitLineCases))]
    public void SplitLines_NormalizesLineEndingsWithoutLosingEmptyLines(string text, string[] expected)
    {
        Assert.Equal(expected, LineDiffEngine.SplitLines(text));
    }

    [Fact]
    public void Build_AlignsReplacementForSideBySideRendering()
    {
        var model = LineDiffEngine.Build("before\nold one\nold two\nafter\n", "before\nnew one\nafter\n");

        Assert.Collection(
            model.Rows,
            row => Assert.False(row.IsChanged),
            row => Assert.Equal((1, 1, true), (row.OldIndex, row.NewIndex, row.IsChanged)),
            row => Assert.Equal((2, (int?)null, true), (row.OldIndex, row.NewIndex, row.IsChanged)),
            row => Assert.False(row.IsChanged));
    }

    [Fact]
    public void BuildUnaligned_StreamsTheSameRowsWithoutRetainingThem()
    {
        const string oldText = "before\nold one\nold two\nafter\n";
        const string newText = "before\nnew one\nafter\n";
        var aligned = LineDiffEngine.Build(oldText, newText);

        var streamed = LineDiffEngine.BuildUnaligned(oldText, newText);

        Assert.Empty(streamed.Rows);
        Assert.Equal(aligned.Blocks, streamed.Blocks);
        Assert.Equal(aligned.Rows, LineDiffEngine.EnumerateRows(streamed));
    }

    [Fact]
    public void Build_ReconstructsDeterministicRandomEdits()
    {
        var random = new Random(917_431);
        for (var iteration = 0; iteration < 100; iteration++)
        {
            var oldLines = Enumerable.Range(0, random.Next(0, 80))
                .Select(index => $"line-{index % 11}").ToList();
            var newLines = new List<string>(oldLines);
            for (var edit = 0; edit < 12; edit++)
            {
                var index = random.Next(0, newLines.Count + 1);
                if (newLines.Count > 0 && random.Next(2) == 0)
                    newLines.RemoveAt(Math.Min(index, newLines.Count - 1));
                else
                    newLines.Insert(index, $"new-{iteration}-{edit}");
            }

            var model = LineDiffEngine.Build(string.Join('\n', oldLines), string.Join('\n', newLines));

            Assert.Equal(newLines, Apply(model));
        }
    }

    [Fact]
    public void Build_AnchoredLargeEditsPreserveInsertionsDeletionsAndRepeatedLines()
    {
        var oldLines = Enumerable.Range(0, 6_000)
            .Select(index => index % 97 == 0 ? "repeated context" : $"unique-{index}")
            .ToList();
        var newLines = new List<string>(oldLines);
        for (var index = 10; index < newLines.Count; index += 10)
            newLines[index] = $"changed-{index}";
        newLines.RemoveRange(2_900, 40);
        newLines.InsertRange(3_500, Enumerable.Range(0, 60).Select(index => $"inserted-{index}"));

        var model = LineDiffEngine.Build(string.Join('\n', oldLines), string.Join('\n', newLines));
        var unaligned = LineDiffEngine.BuildUnaligned(
            string.Join('\n', oldLines), string.Join('\n', newLines));

        Assert.Equal(newLines, Apply(model));
        Assert.Equal(model.Blocks, unaligned.Blocks);
        Assert.Equal(model.Rows, LineDiffEngine.EnumerateRows(unaligned));
    }

    [Fact]
    public void Build_CompletelyDifferentLargeInputsProduceOneReplacement()
    {
        var oldText = string.Join('\n', Enumerable.Range(0, 4_000).Select(index => $"old {index}"));
        var newText = string.Join('\n', Enumerable.Range(0, 4_000).Select(index => $"new {index}"));

        var block = Assert.Single(LineDiffEngine.BuildUnaligned(oldText, newText).Blocks);

        Assert.Equal(new LineDiffBlock(0, 0, 4_000, 4_000), block);
    }

    [Fact]
    public void ChangeSpans_UseTheSameEngineForCharacterChanges()
    {
        var row = new LineDiffRow(0, 0, isChanged: true);

        var spans = LineDiffRendering.ChangeSpans("hello world", "hello LovelyGit", row);

        Assert.NotEmpty(spans.Old);
        Assert.NotEmpty(spans.New);
        Assert.All(spans.Old, span => Assert.Equal("Deleted", span.ChangeType));
        Assert.All(spans.New, span => Assert.Equal("Inserted", span.ChangeType));
        Assert.All(spans.Old, span => Assert.InRange(span.Start + span.Length, 1, "hello world".Length));
        Assert.All(spans.New, span => Assert.InRange(span.Start + span.Length, 1, "hello LovelyGit".Length));
    }

    [Fact]
    public void UnifiedRenderer_ProducesApplicableContextHunks()
    {
        var patch = UnifiedDiffRenderer.Render(
            "one\ntwo\nthree\nfour\n",
            "one\nchanged\nthree\nfour\nadded\n",
            "a/file.txt",
            "b/file.txt",
            context: 1);

        var normalized = patch.ReplaceLineEndings("\n");
        Assert.Contains("--- a/file.txt\n+++ b/file.txt\n", normalized, StringComparison.Ordinal);
        Assert.Contains("-two\n+changed\n", normalized, StringComparison.Ordinal);
        Assert.Contains("+added\n", normalized, StringComparison.Ordinal);
    }

    [Fact]
    public void UnifiedRenderer_RepresentsFinalNewlineOnlyChange()
    {
        var patch = UnifiedDiffRenderer.Render("same text\n", "same text", "a/file.txt", "b/file.txt", 3)
            .ReplaceLineEndings("\n");

        Assert.Contains("-same text\n+same text\n\\ No newline at end of file\n", patch, StringComparison.Ordinal);
    }

    private static string[] Apply(LineDiffModel model)
    {
        var result = new List<string>();
        var oldIndex = 0;
        foreach (var block in model.Blocks)
        {
            result.AddRange(model.OldLines[oldIndex..block.OldStart]);
            result.AddRange(model.NewLines[block.NewStart..(block.NewStart + block.NewCount)]);
            oldIndex = block.OldStart + block.OldCount;
        }
        result.AddRange(model.OldLines[oldIndex..]);
        return result.ToArray();
    }

}
