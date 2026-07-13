using ExpressThat.LovelyGit.Services.Git.CommitGraph;
using ExpressThat.LovelyGit.Services.Git.CommitGraph.Models;
using ExpressThat.LovelyGit.Services.Git.Diffing;

namespace LovelyGit.Tests.Git.CommitGraph;

public sealed class CommitFileDiffPreparationPolicyTests
{
    [Fact]
    public void SelectFiles_ReturnsAllFilesWhenCommitIsSmall()
    {
        var changedFiles = BuildChangedFiles(3);

        var selected = CommitFileDiffPreparationPolicy.SelectFiles(changedFiles);

        Assert.Same(changedFiles, selected);
    }

    [Fact]
    public void SelectFiles_LimitsLargeCommitsToLeadingVisibleFiles()
    {
        var changedFiles = BuildChangedFiles(10);

        var selected = CommitFileDiffPreparationPolicy.SelectFiles(changedFiles);

        Assert.Equal(CommitFileDiffPreparationPolicy.MaxPreparedFileCount, selected.Count);
        Assert.Equal("file-0.txt", selected[0].Path);
        Assert.Equal("file-5.txt", selected[^1].Path);
    }

    [Fact]
    public void CanPersistPreparedText_RejectsLargeModifiedPayloads()
    {
        var oldText = new string('a', DiffInputGuard.FastDiffInputCharacters / 2 + 1);
        var newText = new string('b', DiffInputGuard.FastDiffInputCharacters / 2 + 1);

        Assert.False(CommitFileDiffPreparationPolicy.CanPersistPreparedText(oldText, newText));
    }

    [Fact]
    public void CanPersistPreparedText_KeepsLargeAdditionsAndDeletionsEligible()
    {
        var text = new string('a', DiffInputGuard.FastDiffInputCharacters + 1);

        Assert.True(CommitFileDiffPreparationPolicy.CanPersistPreparedText(string.Empty, text));
        Assert.True(CommitFileDiffPreparationPolicy.CanPersistPreparedText(text, string.Empty));
    }

    [Fact]
    public void CanPersistPreparedText_KeepsNormalModifiedFilesEligible()
    {
        Assert.True(CommitFileDiffPreparationPolicy.CanPersistPreparedText("before", "after"));
    }

    private static List<CommitChangedFile> BuildChangedFiles(int count)
    {
        return Enumerable
            .Range(0, count)
            .Select(index => new CommitChangedFile
            {
                Path = $"file-{index}.txt",
                Status = "Modified",
            })
            .ToList();
    }
}
