using ExpressThat.LovelyGit.Services.Git.CommitGraph;
using ExpressThat.LovelyGit.Services.Git.CommitGraph.Models;

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
