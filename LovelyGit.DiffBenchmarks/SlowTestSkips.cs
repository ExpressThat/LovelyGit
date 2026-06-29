using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace LovelyGit.DiffBenchmarks;

internal sealed partial class SlowTestSkips
{
    private readonly Dictionary<string, BenchmarkResult> results;

    private SlowTestSkips(Dictionary<string, BenchmarkResult> results)
    {
        this.results = results;
    }

    public static SlowTestSkips Load(BenchmarkOptions options)
    {
        var results = ReadCache(options);
        ImportFromHtmlReport(options, results);
        WriteCache(options, results);
        return new SlowTestSkips(results);
    }

    public static void Update(BenchmarkOptions options, IReadOnlyList<BenchmarkResult> results)
    {
        var cached = ReadCache(options);
        foreach (var result in results.Where(result => ShouldPersist(result, options.SlowSkipThresholdMs)))
        {
            cached[Key(result)] = result;
        }

        WriteCache(options, cached);
    }

    public bool TryReuse(
        BenchmarkCandidate candidate,
        BenchmarkCase benchmarkCase,
        CommitDiffViewMode viewMode,
        out BenchmarkResult result)
    {
        if (candidate.Name == "LovelyGit Prototype")
        {
            result = null!;
            return false;
        }

        if (results.TryGetValue(Key(candidate.Name, benchmarkCase.Name, benchmarkCase.LineCount, viewMode.ToString()), out var cached))
        {
            result = cached with
            {
                Status = "ReusedSlow",
                Notes = $"Reused previous {cached.Status} result: {cached.Notes}",
            };
            return true;
        }

        result = null!;
        return false;
    }

    private static Dictionary<string, BenchmarkResult> ReadCache(BenchmarkOptions options)
    {
        var results = new Dictionary<string, BenchmarkResult>(StringComparer.Ordinal);
        var path = CachePath(options);
        if (!File.Exists(path))
        {
            return results;
        }

        foreach (var line in File.ReadLines(path))
        {
            var result = JsonSerializer.Deserialize(line, BenchmarkJsonContext.Default.BenchmarkResult);
            if (result is not null)
            {
                results[Key(result)] = result;
            }
        }

        return results;
    }

    private static void ImportFromHtmlReport(BenchmarkOptions options, Dictionary<string, BenchmarkResult> results)
    {
        var path = Path.Combine(options.ArtifactDirectory, "diff-engine-benchmark-report.html");
        if (!File.Exists(path))
        {
            return;
        }

        foreach (Match match in ResultRegex().Matches(File.ReadAllText(path)))
        {
            var result = ToResult(match);
            if (ShouldPersist(result, options.SlowSkipThresholdMs))
            {
                results[Key(result)] = result;
            }
        }
    }

    private static BenchmarkResult ToResult(Match match)
    {
        return new BenchmarkResult(
            match.Groups["candidate"].Value,
            "",
            match.Groups["case"].Value,
            int.Parse(match.Groups["lines"].Value, CultureInfo.InvariantCulture),
            match.Groups["view"].Value,
            false.ToString(),
            match.Groups["status"].Value,
            double.Parse(match.Groups["diff"].Value, CultureInfo.InvariantCulture),
            double.Parse(match.Groups["serialize"].Value, CultureInfo.InvariantCulture),
            long.Parse(match.Groups["payload"].Value, CultureInfo.InvariantCulture),
            long.Parse(match.Groups["memory"].Value, CultureInfo.InvariantCulture),
            int.Parse(match.Groups["rows"].Value, CultureInfo.InvariantCulture),
            match.Groups["notes"].Value);
    }

    private static bool ShouldPersist(BenchmarkResult result, int thresholdMs)
    {
        return result.DiffMs + result.SerializeMs > thresholdMs
            || result.Status is "TimedOut" or "MemoryLimit";
    }

    private static void WriteCache(BenchmarkOptions options, Dictionary<string, BenchmarkResult> results)
    {
        var lines = results.OrderBy(pair => pair.Key, StringComparer.Ordinal)
            .Select(pair => JsonSerializer.Serialize(pair.Value, BenchmarkJsonContext.Default.BenchmarkResult));
        File.WriteAllLines(CachePath(options), lines);
    }

    private static string CachePath(BenchmarkOptions options)
    {
        return Path.Combine(options.ArtifactDirectory, "diff-engine-benchmark-slow-results.jsonl");
    }

    private static string Key(BenchmarkResult result)
    {
        return Key(result.Candidate, result.CaseName, result.LineCount, result.ViewMode);
    }

    private static string Key(string candidate, string caseName, int lineCount, string viewMode)
    {
        return string.Join('\t', candidate, caseName, lineCount.ToString(CultureInfo.InvariantCulture), viewMode);
    }

    [GeneratedRegex("\\{candidate:\\\"(?<candidate>[^\\\"]+)\\\",caseName:\\\"(?<case>[^\\\"]+)\\\",viewMode:\\\"(?<view>[^\\\"]+)\\\",status:\\\"(?<status>[^\\\"]+)\\\",notes:\\\"(?<notes>[^\\\"]*)\\\",lineCount:(?<lines>\\d+),diffMs:(?<diff>[0-9.]+),serializeMs:(?<serialize>[0-9.]+),payloadBytes:(?<payload>\\d+),memoryBytes:(?<memory>\\d+),rows:(?<rows>\\d+)", RegexOptions.CultureInvariant)]
    private static partial Regex ResultRegex();
}
