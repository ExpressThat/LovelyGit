using System.Diagnostics;
using System.Text.Json;

namespace LovelyGit.DiffBenchmarks;

internal static class BenchmarkRunner
{
    public static IReadOnlyList<BenchmarkResult> Run(
        IReadOnlyList<BenchmarkCase> cases,
        IReadOnlyList<BenchmarkCandidate> candidates,
        BenchmarkOptions options)
    {
        var results = new List<BenchmarkResult>();
        foreach (var candidate in candidates)
        {
            foreach (var benchmarkCase in cases)
            {
                foreach (var viewMode in new[] { CommitDiffViewMode.SideBySide, CommitDiffViewMode.Combined })
                {
                    results.Add(RunOne(candidate, benchmarkCase, viewMode, ignoreWhitespace: false, options));
                }
            }
        }

        return results;
    }

    internal static BenchmarkResult RunOne(
        BenchmarkCandidate candidate,
        BenchmarkCase benchmarkCase,
        CommitDiffViewMode viewMode,
        bool ignoreWhitespace,
        BenchmarkOptions options)
    {
        try
        {
            var beforeMemory = GC.GetTotalMemory(forceFullCollection: true);
            var task = Task.Run(() => Measure(candidate, benchmarkCase, viewMode, ignoreWhitespace, options.Iterations));
            if (options.TimeoutMs > 0 && !task.Wait(options.TimeoutMs))
            {
                return TimedOut(candidate, benchmarkCase, viewMode, ignoreWhitespace, options.TimeoutMs);
            }

            var result = task.Result;
            var afterMemory = GC.GetTotalMemory(forceFullCollection: false);
            return result with { MemoryBytes = Math.Max(0, afterMemory - beforeMemory) };
        }
        catch (Exception ex)
        {
            return new BenchmarkResult(
                candidate.Name,
                candidate.Category,
                benchmarkCase.Name,
                benchmarkCase.LineCount,
                viewMode.ToString(),
                ignoreWhitespace.ToString(),
                "Failed",
                0,
                0,
                0,
                0,
                0,
                ex.GetType().Name + ": " + ex.Message);
        }
    }

    private static BenchmarkResult Measure(
        BenchmarkCandidate candidate,
        BenchmarkCase benchmarkCase,
        CommitDiffViewMode viewMode,
        bool ignoreWhitespace,
        int iterations)
    {
        CommitFileDiffResponse? response = null;
        var diffWatch = Stopwatch.StartNew();
        for (var index = 0; index < iterations; index++)
        {
            response = candidate.Run(benchmarkCase, viewMode, ignoreWhitespace);
        }

        diffWatch.Stop();
        var serializeWatch = Stopwatch.StartNew();
        var payloadBytes = 0L;
        byte[]? utf8Json = null;
        if (response?.PayloadByteCountFactory is not null)
        {
            payloadBytes = response.PayloadByteCountFactory();
        }
        else if (response?.Utf8JsonFactory is not null)
        {
            utf8Json = response.Utf8JsonFactory();
            payloadBytes = utf8Json.Length;
        }
        else
        {
            var json = response?.JsonFactory is null
                ? JsonSerializer.Serialize(response, BenchmarkJsonContext.Default.CommitFileDiffResponse)
                : response.JsonFactory();
            payloadBytes = System.Text.Encoding.UTF8.GetByteCount(json);
            if (ShouldValidateJson())
            {
                JsonDocument.Parse(json).Dispose();
            }
        }

        serializeWatch.Stop();
        if (utf8Json is not null && ShouldValidateJson())
        {
            JsonDocument.Parse(utf8Json).Dispose();
        }

        return new BenchmarkResult(
            candidate.Name,
            candidate.Category,
            benchmarkCase.Name,
            benchmarkCase.LineCount,
            viewMode.ToString(),
            ignoreWhitespace.ToString(),
            "Measured",
            diffWatch.Elapsed.TotalMilliseconds / iterations,
            serializeWatch.Elapsed.TotalMilliseconds,
            payloadBytes,
            0,
            response?.PlannedRows ?? response?.Lines.Count ?? 0,
            benchmarkCase.Notes);
    }

    private static bool ShouldValidateJson() =>
        Environment.GetEnvironmentVariable("LOVELYGIT_VALIDATE_BENCHMARK_JSON") == "1";

    private static BenchmarkResult TimedOut(
        BenchmarkCandidate candidate,
        BenchmarkCase benchmarkCase,
        CommitDiffViewMode viewMode,
        bool ignoreWhitespace,
        int timeoutMs)
    {
        return new BenchmarkResult(
            candidate.Name,
            candidate.Category,
            benchmarkCase.Name,
            benchmarkCase.LineCount,
            viewMode.ToString(),
            ignoreWhitespace.ToString(),
            "TimedOut",
            0,
            0,
            0,
            0,
            0,
            $"Exceeded {timeoutMs.ToString(System.Globalization.CultureInfo.InvariantCulture)} ms.");
    }
}
