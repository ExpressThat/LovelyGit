using ExpressThat.LovelyGit.Services.Git.Diffing;

namespace ExpressThat.LovelyGit.Services.Git.FileBlame;

internal static class BlameLineMapper
{
    public static int[] MapNewLinesToOld(string oldText, string newText, int newLineCount)
    {
        var mapping = new int[newLineCount];
        Array.Fill(mapping, -1);
        if (newLineCount == 0)
        {
            return mapping;
        }

        var result = LineDiffEngine.Build(oldText, newText);
        var oldIndex = 0;
        var newIndex = 0;
        foreach (var block in result.Blocks)
        {
            MapUnchanged(
                mapping,
                ref oldIndex,
                ref newIndex,
                block.OldStart,
                block.NewStart);
            oldIndex += block.OldCount;
            newIndex += block.NewCount;
        }

        MapUnchanged(
            mapping,
            ref oldIndex,
            ref newIndex,
            result.OldLines.Length,
            Math.Min(result.NewLines.Length, newLineCount));
        return mapping;
    }

    private static void MapUnchanged(
        int[] mapping,
        ref int oldIndex,
        ref int newIndex,
        int oldEnd,
        int newEnd)
    {
        while (oldIndex < oldEnd && newIndex < newEnd && newIndex < mapping.Length)
        {
            mapping[newIndex++] = oldIndex++;
        }
    }
}
