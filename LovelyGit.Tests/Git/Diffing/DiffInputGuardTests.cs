using ExpressThat.LovelyGit.Services.Git.CommitGraph.Models;
using ExpressThat.LovelyGit.Services.Git.Diffing;

namespace LovelyGit.Tests.Git.Diffing;

public sealed class DiffInputGuardTests
{
    [Fact]
    public void ShouldUseFastDiff_ReturnsFalseForSmallInputs()
    {
        var shouldUseFastDiff = DiffInputGuard.ShouldUseFastDiff("old\n", "new\n");

        Assert.False(shouldUseFastDiff);
    }

    [Fact]
    public void ShouldUseFastDiff_ReturnsTrueWhenCharacterLimitIsExceeded()
    {
        var oversizedText = new string('a', DiffInputGuard.FastDiffInputCharacters + 1);

        var shouldUseFastDiff = DiffInputGuard.ShouldUseFastDiff(oversizedText, string.Empty);

        Assert.True(shouldUseFastDiff);
    }

    [Fact]
    public void ShouldUseFastDiff_ReturnsTrueWhenLineLimitIsExceeded()
    {
        var oversizedText = string.Join('\n', Enumerable.Repeat("line", DiffInputGuard.FastDiffInputLines + 1));

        var shouldUseFastDiff = DiffInputGuard.ShouldUseFastDiff(oversizedText, string.Empty);

        Assert.True(shouldUseFastDiff);
    }
}
