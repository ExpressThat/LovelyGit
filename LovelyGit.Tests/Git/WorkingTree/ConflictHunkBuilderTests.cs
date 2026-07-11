using ExpressThat.LovelyGit.Services.Git.WorkingTree;

namespace LovelyGit.Tests.Git.WorkingTree;

public sealed class ConflictHunkBuilderTests
{
    [Fact]
    public void Build_UsesIndependentSourceLineNumbersAfterOneSidedInsertion()
    {
        var hunks = ConflictHunkBuilder.Build(
            "shared\nbase\nafter\n",
            "shared\ncurrent insert one\ncurrent insert two\ncurrent\nafter\n",
            "shared\nincoming\nafter\n",
            "shared\ncurrent insert one\ncurrent insert two\n<<<<<<< HEAD\ncurrent\n=======\nincoming\n>>>>>>> feature\nafter\n");

        var hunk = Assert.Single(hunks);
        Assert.Equal(2, hunk.BaseStartLine);
        Assert.Equal(1, hunk.BaseLineCount);
        Assert.Equal(4, hunk.CurrentStartLine);
        Assert.Equal(2, hunk.IncomingStartLine);
    }

    [Fact]
    public void Build_KeepsRepeatedCandidateTextInConflictOrder()
    {
        var hunks = ConflictHunkBuilder.Build(
            "start\nbase\nmiddle\nbase\n",
            "start\nsame\nmiddle\nsame\n",
            "start\nother\nmiddle\nother\n",
            "start\n<<<<<<< HEAD\nsame\n=======\nother\n>>>>>>> one\nmiddle\n<<<<<<< HEAD\nsame\n=======\nother\n>>>>>>> two\n");

        Assert.Equal([2, 4], hunks.Select(hunk => hunk.CurrentStartLine));
        Assert.Equal([2, 4], hunks.Select(hunk => hunk.IncomingStartLine));
        Assert.Equal([0, 1], hunks.Select(hunk => hunk.Id));
    }

    [Fact]
    public void Build_RepresentsAnEmptyCandidateAsASelectableInsertionPoint()
    {
        var hunks = ConflictHunkBuilder.Build(
            "before\nremoved\nafter\n",
            "before\nafter\n",
            "before\nincoming\nafter\n",
            "before\n<<<<<<< HEAD\n=======\nincoming\n>>>>>>> feature\nafter\n");

        var hunk = Assert.Single(hunks);
        Assert.Equal(2, hunk.CurrentStartLine);
        Assert.Equal(0, hunk.CurrentLineCount);
        Assert.Equal(2, hunk.IncomingStartLine);
        Assert.Equal(1, hunk.IncomingLineCount);
        Assert.Equal(2, hunk.BaseStartLine);
    }

    [Theory]
    [InlineData("one\r\ntwo\r\n", 2)]
    [InlineData("one\ntwo", 2)]
    [InlineData("", 0)]
    public void SplitLines_PreservesPhysicalLineCount(string text, int expected)
    {
        Assert.Equal(expected, ConflictHunkBuilder.SplitLines(text).Count);
    }

    [Fact]
    public void Build_MapsAddAddConflictWithoutInventingBaseContent()
    {
        var hunk = Assert.Single(ConflictHunkBuilder.Build(
            string.Empty,
            "current added\n",
            "incoming added\n",
            "<<<<<<< HEAD\ncurrent added\n=======\nincoming added\n>>>>>>> feature\n"));

        Assert.Equal(0, hunk.BaseLineCount);
        Assert.Equal(1, hunk.CurrentStartLine);
        Assert.Equal(1, hunk.CurrentLineCount);
        Assert.Equal(1, hunk.IncomingStartLine);
        Assert.Equal(1, hunk.IncomingLineCount);
    }

    [Fact]
    public void Build_MapsModifyDeleteAndCrLfWithoutFinalNewline()
    {
        var hunk = Assert.Single(ConflictHunkBuilder.Build(
            "before\r\nbase\r\nafter",
            "before\r\ncurrent\r\nafter",
            "before\r\nafter",
            "before\r\n<<<<<<< HEAD\r\ncurrent\r\n=======\r\n>>>>>>> feature\r\nafter"));

        Assert.Equal(2, hunk.BaseStartLine);
        Assert.Equal(1, hunk.BaseLineCount);
        Assert.Equal(2, hunk.CurrentStartLine);
        Assert.Equal(1, hunk.CurrentLineCount);
        Assert.Equal(2, hunk.IncomingStartLine);
        Assert.Equal(0, hunk.IncomingLineCount);
    }
}
