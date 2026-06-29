using System.Diagnostics;
using System.Text.Json;

namespace LovelyGit.DiffBenchmarks;

internal static class IsolatedBenchmarkRunner
{
    public static IReadOnlyList<BenchmarkResult> Run(
        IReadOnlyList<BenchmarkCandidate> candidates,
        BenchmarkOptions options,
        SlowTestSkips slowSkips)
    {
        var results = new List<BenchmarkResult>();
        var chromiumCases = ChromiumBenchmarkFixtures.Create(options.ChromiumRepoPath);
        var lovelyGitOnlyCases = candidates.Any(IsLovelyGit)
            ? new[] { VirtualBillionBenchmarkFixtures.Create() }
            : [];
        var total = (candidates.Count * (options.LineCounts.Length * BenchmarkFixtures.CaseCountPerLineCount + chromiumCases.Count)
            + lovelyGitOnlyCases.Length) * 2;
        var index = 0;
        var logPath = ProgressLogPath(options);
        File.WriteAllText(logPath, $"Diff benchmark started {DateTimeOffset.Now:O}{Environment.NewLine}");
        foreach (var lineCount in options.LineCounts)
        {
            Log(options, $"GENERATE fixtures lineCount={lineCount}");
            var cases = BenchmarkFixtures.Create([lineCount]);
            RunCases(candidates, options, slowSkips, results, cases, total, ref index);
            cases = [];
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            Log(options, $"CLEAR fixtures lineCount={lineCount}");
        }

        Log(options, $"GENERATE chromium fixtures count={chromiumCases.Count}");
        RunCases(candidates, options, slowSkips, results, chromiumCases, total, ref index);
        Log(options, "CLEAR chromium fixtures");
        if (lovelyGitOnlyCases.Length > 0)
        {
            Log(options, $"GENERATE lovelygit-only fixtures count={lovelyGitOnlyCases.Length}");
            RunCases(candidates.Where(IsLovelyGit).ToArray(), options, slowSkips, results, lovelyGitOnlyCases, total, ref index);
            Log(options, "CLEAR lovelygit-only fixtures");
        }

        return results;
    }

    private static bool IsLovelyGit(BenchmarkCandidate candidate)
    {
        return candidate.Name == "LovelyGit Prototype";
    }

    private static void RunCases(
        IReadOnlyList<BenchmarkCandidate> candidates,
        BenchmarkOptions options,
        SlowTestSkips slowSkips,
        List<BenchmarkResult> results,
        IReadOnlyList<BenchmarkCase> cases,
        int total,
        ref int index)
    {
        foreach (var candidate in candidates)
        {
            foreach (var benchmarkCase in cases)
            {
                foreach (var viewMode in new[] { CommitDiffViewMode.SideBySide, CommitDiffViewMode.Combined })
                {
                    index++;
                    if (slowSkips.TryReuse(candidate, benchmarkCase, viewMode, out var reused))
                    {
                        Log(options, $"SKIP  {index}/{total} slow {Label(candidate, benchmarkCase, viewMode)}");
                        results.Add(reused);
                        continue;
                    }

                    Log(options, $"START {index}/{total} {Label(candidate, benchmarkCase, viewMode)}");
                    var result = RunIsolated(candidate, benchmarkCase, viewMode, options);
                    Log(options, $"END   {index}/{total} {result.Status} {Label(candidate, benchmarkCase, viewMode)} {result.Notes}");
                    results.Add(result);
                }
            }
        }
    }

    private static BenchmarkResult RunIsolated(
        BenchmarkCandidate candidate,
        BenchmarkCase benchmarkCase,
        CommitDiffViewMode viewMode,
        BenchmarkOptions options)
    {
        var resultPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.json");
        using var process = StartChild(candidate, benchmarkCase, viewMode, options, resultPath);
        var maxMemory = 0L;
        var nextProgressLogMs = 5_000L;
        var watch = Stopwatch.StartNew();
        while (!process.HasExited)
        {
            try
            {
                process.Refresh();
                maxMemory = Math.Max(maxMemory, process.WorkingSet64);
            }
            catch (InvalidOperationException)
            {
                break;
            }

            if (watch.ElapsedMilliseconds >= nextProgressLogMs)
            {
                Log(options,
                    $"RUN   {Label(candidate, benchmarkCase, viewMode)} elapsed={watch.ElapsedMilliseconds}ms ram={Bytes(maxMemory)}");
                nextProgressLogMs += 5_000;
            }

            if (maxMemory > options.MemoryLimitBytes)
            {
                process.Kill(entireProcessTree: true);
                Log(options, $"KILL  memory {Label(candidate, benchmarkCase, viewMode)} ram={Bytes(maxMemory)}");
                return Limited(candidate, benchmarkCase, viewMode, maxMemory, options.MemoryLimitBytes);
            }

            if (options.TimeoutMs > 0 && watch.ElapsedMilliseconds > options.TimeoutMs)
            {
                process.Kill(entireProcessTree: true);
                Log(options, $"KILL  timeout {Label(candidate, benchmarkCase, viewMode)} ram={Bytes(maxMemory)}");
                return TimedOut(candidate, benchmarkCase, viewMode, maxMemory, options.TimeoutMs);
            }

            Thread.Sleep(50);
        }

        return ReadResult(candidate, benchmarkCase, viewMode, resultPath, maxMemory);
    }

    private static Process StartChild(
        BenchmarkCandidate candidate,
        BenchmarkCase benchmarkCase,
        CommitDiffViewMode viewMode,
        BenchmarkOptions options,
        string resultPath)
    {
        var startInfo = new ProcessStartInfo(Environment.ProcessPath!)
        {
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        startInfo.ArgumentList.Add("--lines");
        startInfo.ArgumentList.Add(benchmarkCase.LineCount.ToString());
        startInfo.ArgumentList.Add("--iterations");
        startInfo.ArgumentList.Add(options.Iterations.ToString());
        startInfo.ArgumentList.Add("--timeout-ms");
        startInfo.ArgumentList.Add(options.TimeoutMs.ToString());
        startInfo.ArgumentList.Add("--child-candidate");
        startInfo.ArgumentList.Add(candidate.Name);
        startInfo.ArgumentList.Add("--child-case");
        startInfo.ArgumentList.Add(benchmarkCase.Name);
        startInfo.ArgumentList.Add("--child-line-count");
        startInfo.ArgumentList.Add(benchmarkCase.LineCount.ToString());
        startInfo.ArgumentList.Add("--child-view-mode");
        startInfo.ArgumentList.Add(viewMode.ToString());
        startInfo.ArgumentList.Add("--child-result");
        startInfo.ArgumentList.Add(resultPath);
        return Process.Start(startInfo)!;
    }

    private static BenchmarkResult ReadResult(
        BenchmarkCandidate candidate,
        BenchmarkCase benchmarkCase,
        CommitDiffViewMode viewMode,
        string resultPath,
        long maxMemory)
    {
        if (!File.Exists(resultPath))
        {
            return Failed(candidate, benchmarkCase, viewMode, maxMemory, "Child exited without a result.");
        }

        var json = File.ReadAllText(resultPath);
        File.Delete(resultPath);
        var result = JsonSerializer.Deserialize(json, BenchmarkJsonContext.Default.BenchmarkResult)!;
        return result with { MemoryBytes = Math.Max(result.MemoryBytes, maxMemory) };
    }

    private static void Log(BenchmarkOptions options, string message)
    {
        var line = $"{DateTimeOffset.Now:O} {message}{Environment.NewLine}";
        Console.Write(line);
        File.AppendAllText(ProgressLogPath(options), line);
    }

    private static string ProgressLogPath(BenchmarkOptions options)
    {
        return Path.Combine(options.ArtifactDirectory, "diff-engine-benchmark-progress.log");
    }

    private static string Label(
        BenchmarkCandidate candidate,
        BenchmarkCase benchmarkCase,
        CommitDiffViewMode viewMode)
    {
        return $"{candidate.Name} | {benchmarkCase.Name} | {benchmarkCase.LineCount} | {viewMode}";
    }

    private static BenchmarkResult Limited(
        BenchmarkCandidate candidate,
        BenchmarkCase benchmarkCase,
        CommitDiffViewMode viewMode,
        long maxMemory,
        long limit)
    {
        return Result(candidate, benchmarkCase, viewMode, "MemoryLimit", maxMemory, $"Exceeded {Bytes(limit)} memory limit.");
    }

    private static BenchmarkResult TimedOut(
        BenchmarkCandidate candidate,
        BenchmarkCase benchmarkCase,
        CommitDiffViewMode viewMode,
        long maxMemory,
        int timeoutMs)
    {
        return Result(candidate, benchmarkCase, viewMode, "TimedOut", maxMemory, $"Exceeded {timeoutMs} ms timeout.");
    }

    private static BenchmarkResult Failed(
        BenchmarkCandidate candidate,
        BenchmarkCase benchmarkCase,
        CommitDiffViewMode viewMode,
        long memory,
        string notes)
    {
        return Result(candidate, benchmarkCase, viewMode, "Failed", memory, notes);
    }

    private static BenchmarkResult Result(
        BenchmarkCandidate candidate,
        BenchmarkCase benchmarkCase,
        CommitDiffViewMode viewMode,
        string status,
        long memory,
        string notes)
    {
        return new BenchmarkResult(candidate.Name, candidate.Category, benchmarkCase.Name, benchmarkCase.LineCount,
            viewMode.ToString(), false.ToString(), status, 0, 0, 0, memory, 0, notes);
    }

    private static string Bytes(long value)
    {
        return $"{value / 1024d / 1024d / 1024d:0.##} GB";
    }
}
