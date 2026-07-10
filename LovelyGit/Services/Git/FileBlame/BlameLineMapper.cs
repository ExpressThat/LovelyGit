using DiffPlex;

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

        var result = new Differ().CreateLineDiffs(
            oldText,
            newText,
            ignoreWhitespace: false,
            ignoreCase: false);
        var oldIndex = 0;
        var newIndex = 0;
        foreach (var block in result.DiffBlocks)
        {
            MapUnchanged(
                mapping,
                ref oldIndex,
                ref newIndex,
                block.DeleteStartA,
                block.InsertStartB);
            oldIndex += block.DeleteCountA;
            newIndex += block.InsertCountB;
        }

        MapUnchanged(
            mapping,
            ref oldIndex,
            ref newIndex,
            result.PiecesOld.Count,
            Math.Min(result.PiecesNew.Count, newLineCount));
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
