using ExpressThat.LovelyGit.Services.Git.FileBlame;

namespace LovelyGit.Tests.Git.FileBlame;

public sealed class BlameLineMapperTests
{
    [Fact]
    public void MapNewLinesToOld_MapsInsertionsWithoutShiftingOwnership()
    {
        var mapping = BlameLineMapper.MapNewLinesToOld(
            "one\ntwo\nthree\n",
            "one\ninserted\ntwo\nthree\n",
            newLineCount: 4);

        Assert.Equal([0, -1, 1, 2], mapping);
    }

    [Fact]
    public void MapNewLinesToOld_MapsRepeatedLinesThroughSmallGapLcs()
    {
        var mapping = BlameLineMapper.MapNewLinesToOld(
            "one\nrepeat\nrepeat\nthree\n",
            "one\nrepeat\ninserted\nrepeat\nthree\n",
            newLineCount: 5);

        Assert.Equal([0, 1, -1, 2, 3], mapping);
    }

}
