namespace LovelyGit.DiffBenchmarks;

internal static class BenchmarkFixtures
{
    public const int CaseCountPerLineCount = 7;

    public static IReadOnlyList<BenchmarkCase> Create(IEnumerable<int> lineCounts)
    {
        var cases = new List<BenchmarkCase>();
        foreach (var lineCount in lineCounts)
        {
            var baseLines = CreateLines(lineCount, "base");
            var top = ReplaceLine(baseLines, 0, "changed top");
            var middle = ReplaceLine(baseLines, lineCount / 2, "changed middle");
            var bottom = ReplaceLine(baseLines, Math.Max(0, lineCount - 1), "changed bottom");
            cases.Add(new BenchmarkCase("added", lineCount, string.Empty, Join(CreateLines(lineCount, "added")), "new file", IsAscii: true));
            cases.Add(new BenchmarkCase("deleted", lineCount, Join(baseLines), string.Empty, "deleted file", IsAscii: true));
            cases.Add(new BenchmarkCase("modified-top", lineCount, Join(baseLines), Join(top), "one top edit", IsAscii: true));
            cases.Add(new BenchmarkCase("modified-middle", lineCount, Join(baseLines), Join(middle), "one middle edit", IsAscii: true));
            cases.Add(new BenchmarkCase("modified-bottom", lineCount, Join(baseLines), Join(bottom), "one bottom edit", IsAscii: true));
            cases.Add(new BenchmarkCase("repeated", lineCount, Join(CreateRepeated(lineCount, false)), Join(CreateRepeated(lineCount, true)), "repeated lines", IsAscii: true));
            cases.Add(new BenchmarkCase("unicode-modified", lineCount, Join(CreateUnicode(lineCount, false)), Join(CreateUnicode(lineCount, true)), "UTF-8 text with one edit", IsAscii: false));
        }

        return cases;
    }

    private static string[] CreateLines(int count, string prefix)
    {
        return Enumerable.Range(0, count).Select(index => $"{prefix} line {index:000000}").ToArray();
    }

    private static string[] CreateRepeated(int count, bool changed)
    {
        return Enumerable.Range(0, count)
            .Select(index => index % 100 == 0 && changed ? $"changed block {index}" : $"repeat {index % 25}")
            .ToArray();
    }

    private static string[] CreateUnicode(int count, bool changed)
    {
        return Enumerable.Range(0, count)
            .Select(index => index == count / 2 && changed ? $"cafe delta 東京 {index}" : $"cafe 東京 {index}")
            .ToArray();
    }

    private static string[] ReplaceLine(string[] lines, int index, string value)
    {
        var copy = (string[])lines.Clone();
        if (copy.Length > 0)
        {
            copy[index] = value;
        }

        return copy;
    }

    private static string Join(IEnumerable<string> lines)
    {
        return string.Join('\n', lines);
    }
}
