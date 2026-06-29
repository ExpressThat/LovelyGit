using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace LovelyGit.DiffBenchmarks;

internal sealed partial class BenchmarkResultMerger
{
    public static IReadOnlyList<BenchmarkResult> MergeWithExisting(
        BenchmarkOptions options,
        IReadOnlyList<BenchmarkResult> freshResults)
    {
        if (options.CandidateNames.Length == 0)
        {
            return freshResults;
        }

        var path = Path.Combine(options.ArtifactDirectory, "diff-engine-benchmark-report.html");
        if (!File.Exists(path))
        {
            return freshResults;
        }

        var merged = ReadExisting(path);
        foreach (var result in freshResults)
        {
            merged[Key(result)] = result;
        }

        return merged.Values
            .OrderBy(result => result.LineCount)
            .ThenBy(result => result.CaseName, StringComparer.Ordinal)
            .ThenBy(result => result.Candidate, StringComparer.Ordinal)
            .ThenBy(result => result.ViewMode, StringComparer.Ordinal)
            .ToArray();
    }

    private static Dictionary<string, BenchmarkResult> ReadExisting(string path)
    {
        var results = new Dictionary<string, BenchmarkResult>(StringComparer.Ordinal);
        var content = File.ReadAllText(path);
        if (TryReadJsonResults(content, results))
        {
            return results;
        }

        foreach (Match match in ResultRegex().Matches(content))
        {
            var result = new BenchmarkResult(
                Decode(match.Groups["candidate"].Value),
                "",
                Decode(match.Groups["case"].Value),
                int.Parse(match.Groups["lines"].Value, CultureInfo.InvariantCulture),
                Decode(match.Groups["view"].Value),
                false.ToString(),
                Decode(match.Groups["status"].Value),
                double.Parse(match.Groups["diff"].Value, CultureInfo.InvariantCulture),
                double.Parse(match.Groups["serialize"].Value, CultureInfo.InvariantCulture),
                long.Parse(match.Groups["payload"].Value, CultureInfo.InvariantCulture),
                long.Parse(match.Groups["memory"].Value, CultureInfo.InvariantCulture),
                int.Parse(match.Groups["rows"].Value, CultureInfo.InvariantCulture),
                Decode(match.Groups["notes"].Value));
            results[Key(result)] = result;
        }

        return results;
    }

    private static bool TryReadJsonResults(string content, Dictionary<string, BenchmarkResult> results)
    {
        var marker = "const results=";
        var start = content.IndexOf(marker, StringComparison.Ordinal);
        if (start < 0)
        {
            return false;
        }

        start += marker.Length;
        var end = FindJsonArrayEnd(content, start);
        if (end < 0)
        {
            return false;
        }

        ReportRow[]? rows;
        try
        {
            rows = JsonSerializer.Deserialize(
                content.AsSpan(start, end - start + 1),
                BenchmarkJsonContext.Default.ReportRowArray);
        }
        catch (JsonException)
        {
            return false;
        }

        if (rows is null)
        {
            return false;
        }

        foreach (var row in rows)
        {
            var result = new BenchmarkResult(
                row.Candidate,
                "",
                row.CaseName,
                row.LineCount,
                row.ViewMode,
                false.ToString(),
                row.Status,
                row.DiffMs,
                row.SerializeMs,
                row.PayloadBytes,
                row.MemoryBytes,
                row.Rows,
                row.Notes);
            results[Key(result)] = result;
        }

        return results.Count > 0;
    }

    private static int FindJsonArrayEnd(string content, int start)
    {
        var inString = false;
        var escaped = false;
        var depth = 0;
        for (var index = start; index < content.Length; index++)
        {
            var ch = content[index];
            if (escaped)
            {
                escaped = false;
                continue;
            }

            if (inString)
            {
                if (ch == '\\')
                {
                    escaped = true;
                }
                else if (ch == '"')
                {
                    inString = false;
                }

                continue;
            }

            if (ch == '"')
            {
                inString = true;
                continue;
            }

            if (ch == '[')
            {
                depth++;
                continue;
            }

            if (ch == ']')
            {
                depth--;
                if (depth == 0)
                {
                    return index;
                }
            }
        }

        return -1;
    }

    private static string Decode(string value)
    {
        return JavaScriptStringDecoder.Decode(value);
    }

    private static string Key(BenchmarkResult result)
    {
        return string.Join(
            '\t',
            result.Candidate,
            result.CaseName,
            result.LineCount.ToString(CultureInfo.InvariantCulture),
            result.ViewMode);
    }

    [GeneratedRegex("\\{candidate:\\\"(?<candidate>[^\\\"]+)\\\",caseName:\\\"(?<case>[^\\\"]+)\\\",viewMode:\\\"(?<view>[^\\\"]+)\\\",status:\\\"(?<status>[^\\\"]+)\\\",notes:\\\"(?<notes>[^\\\"]*)\\\",lineCount:(?<lines>\\d+),diffMs:(?<diff>[0-9.]+),serializeMs:(?<serialize>[0-9.]+),payloadBytes:(?<payload>\\d+),memoryBytes:(?<memory>\\d+),rows:(?<rows>\\d+)", RegexOptions.CultureInvariant)]
    private static partial Regex ResultRegex();
}
