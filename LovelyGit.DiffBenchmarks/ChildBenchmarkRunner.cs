using System.Text.Json;

namespace LovelyGit.DiffBenchmarks;

internal static class ChildBenchmarkRunner
{
    public static int Run(
        IReadOnlyList<BenchmarkCase> cases,
        IReadOnlyList<BenchmarkCandidate> candidates,
        BenchmarkOptions options)
    {
        var candidate = candidates.Single(candidate => candidate.Name == options.ChildCandidate);
        var benchmarkCase = FindCase(cases, options);
        var viewMode = Enum.Parse<CommitDiffViewMode>(options.ChildViewMode!);
        var result = BenchmarkRunner.RunOne(
            candidate,
            benchmarkCase,
            viewMode,
            ignoreWhitespace: false,
            options);
        var json = JsonSerializer.Serialize(result, BenchmarkJsonContext.Default.BenchmarkResult);
        File.WriteAllText(options.ChildResultPath!, json);
        return result.Status == "Failed" ? 1 : 0;
    }

    private static BenchmarkCase FindCase(
        IReadOnlyList<BenchmarkCase> cases,
        BenchmarkOptions options)
    {
        var benchmarkCase = cases.FirstOrDefault(test =>
            test.Name == options.ChildCase && test.LineCount == options.ChildLineCount);
        if (benchmarkCase is not null)
        {
            return benchmarkCase;
        }

        if (options.ChildCase == VirtualBillionBenchmarkFixtures.CaseName)
        {
            return VirtualBillionBenchmarkFixtures.Create();
        }

        return ChromiumBenchmarkFixtures.Create(options.ChromiumRepoPath).Single(test =>
            test.Name == options.ChildCase && test.LineCount == options.ChildLineCount);
    }
}
