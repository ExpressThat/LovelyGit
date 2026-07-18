using System.Text;

namespace ExpressThat.LovelyGit.Services.Git.Diffing;

internal static class UnifiedDiffRenderer
{
    public static string Render(string oldText, string newText, string oldPath, string newPath, int context)
    {
        TryRender(oldText, newText, oldPath, newPath, context, int.MaxValue, out var patch);
        return patch;
    }

    public static bool TryRender(
        string oldText,
        string newText,
        string oldPath,
        string newPath,
        int context,
        int maximumCharacters,
        out string patch)
    {
        var model = LineDiffEngine.Build(oldText, newText);
        var output = new StringBuilder().Append("--- ").AppendLine(oldPath).Append("+++ ").AppendLine(newPath);
        if (output.Length > maximumCharacters)
        {
            patch = string.Empty;
            return false;
        }
        if (!model.HasDifferences)
        {
            patch = output.ToString();
            return true;
        }

        foreach (var group in Group(model.Blocks, context, model.OldLines.Length, model.NewLines.Length))
        {
            if (!AppendGroup(
                    output,
                    model,
                    group,
                    EndsWithNewLine(oldText),
                    EndsWithNewLine(newText),
                    maximumCharacters))
            {
                patch = string.Empty;
                return false;
            }
        }
        patch = output.ToString();
        return true;
    }

    private static IEnumerable<DiffGroup> Group(
        IReadOnlyList<LineDiffBlock> blocks, int context, int oldLength, int newLength)
    {
        DiffGroup? active = null;
        foreach (var block in blocks)
        {
            var next = new DiffGroup(
                Math.Max(0, block.OldStart - context),
                Math.Max(0, block.NewStart - context),
                Math.Min(oldLength, block.OldStart + block.OldCount + context),
                Math.Min(newLength, block.NewStart + block.NewCount + context));
            if (active is not null && next.OldStart <= active.OldEnd && next.NewStart <= active.NewEnd)
            {
                active = active with { OldEnd = next.OldEnd, NewEnd = next.NewEnd };
                continue;
            }
            if (active is not null) yield return active;
            active = next;
        }
        if (active is not null) yield return active;
    }

    private static bool AppendGroup(
        StringBuilder output,
        LineDiffModel model,
        DiffGroup group,
        bool oldEndsWithNewLine,
        bool newEndsWithNewLine,
        int maximumCharacters)
    {
        output.Append("@@ -").Append(Range(group.OldStart, group.OldEnd - group.OldStart))
            .Append(" +").Append(Range(group.NewStart, group.NewEnd - group.NewStart)).AppendLine(" @@");
        if (output.Length > maximumCharacters) return false;
        var oldIndex = group.OldStart;
        var newIndex = group.NewStart;
        foreach (var block in model.Blocks.Where(block => Intersects(block, group)))
        {
            while (oldIndex < block.OldStart && newIndex < block.NewStart)
            {
                if (!AppendLine(output, ' ', model.OldLines[oldIndex],
                        oldIndex == model.OldLines.Length - 1 && !oldEndsWithNewLine,
                        maximumCharacters)) return false;
                oldIndex++;
                newIndex++;
            }
            for (var index = 0; index < block.OldCount; index++)
            {
                var line = block.OldStart + index;
                if (!AppendLine(output, '-', model.OldLines[line],
                        line == model.OldLines.Length - 1 && !oldEndsWithNewLine,
                        maximumCharacters)) return false;
            }
            for (var index = 0; index < block.NewCount; index++)
            {
                var line = block.NewStart + index;
                if (!AppendLine(output, '+', model.NewLines[line],
                        line == model.NewLines.Length - 1 && !newEndsWithNewLine,
                        maximumCharacters)) return false;
            }
            oldIndex = block.OldStart + block.OldCount;
            newIndex = block.NewStart + block.NewCount;
        }
        while (oldIndex < group.OldEnd && newIndex < group.NewEnd)
        {
            if (!AppendLine(output, ' ', model.OldLines[oldIndex],
                    oldIndex == model.OldLines.Length - 1 && !oldEndsWithNewLine,
                    maximumCharacters)) return false;
            oldIndex++;
            newIndex++;
        }
        return true;
    }

    private static bool AppendLine(
        StringBuilder output,
        char prefix,
        string text,
        bool missingNewLine,
        int maximumCharacters)
    {
        const string missingNewLineText = "\\ No newline at end of file";
        var required = 1 + text.Length + Environment.NewLine.Length;
        if (missingNewLine)
            required += missingNewLineText.Length + Environment.NewLine.Length;
        if (required > maximumCharacters - output.Length) return false;

        output.Append(prefix).AppendLine(text);
        if (missingNewLine) output.AppendLine(missingNewLineText);
        return true;
    }

    private static bool EndsWithNewLine(string text) =>
        text.EndsWith('\n') || text.EndsWith('\r');

    private static bool Intersects(LineDiffBlock block, DiffGroup group) =>
        block.OldStart <= group.OldEnd && block.NewStart <= group.NewEnd
        && block.OldStart + block.OldCount >= group.OldStart
        && block.NewStart + block.NewCount >= group.NewStart;

    private static string Range(int zeroBasedStart, int count) => count == 0
        ? $"{zeroBasedStart},0"
        : count == 1 ? (zeroBasedStart + 1).ToString() : $"{zeroBasedStart + 1},{count}";

    private sealed record DiffGroup(int OldStart, int NewStart, int OldEnd, int NewEnd);
}
