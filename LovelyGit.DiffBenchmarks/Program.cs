namespace LovelyGit.DiffBenchmarks;

internal static class Program
{
    public static int Main(string[] args)
    {
        var options = BenchmarkOptions.Parse(args);
        Directory.CreateDirectory(options.ArtifactDirectory);
        var allCandidates = BenchmarkCandidates.Create();
        var runCandidates = FilterCandidates(allCandidates, options);
        if (options.IsChildRun)
        {
            var cases = options.ChildCase == VirtualBillionBenchmarkFixtures.CaseName
                ? []
                : BenchmarkFixtures.Create(options.LineCounts);
            return ChildBenchmarkRunner.Run(cases, runCandidates, options);
        }

        var slowSkips = SlowTestSkips.Load(options);
        var results = IsolatedBenchmarkRunner.Run(runCandidates, options, slowSkips);
        results = BenchmarkResultMerger.MergeWithExisting(options, results);
        SlowTestSkips.Update(options, results);
        var markdownPath = Path.Combine(options.ArtifactDirectory, "diff-engine-benchmark-report.md");
        var htmlPath = Path.Combine(options.ArtifactDirectory, "diff-engine-benchmark-report.html");
        ReportWriter.Write(markdownPath, results, allCandidates, options);
        HtmlReportWriter.Write(htmlPath, results, allCandidates, options);
        Console.WriteLine(markdownPath);
        Console.WriteLine(htmlPath);
        return results.Any(result => result.Status == "Failed") ? 1 : 0;
    }

    private static IReadOnlyList<BenchmarkCandidate> FilterCandidates(
        IReadOnlyList<BenchmarkCandidate> candidates,
        BenchmarkOptions options)
    {
        if (options.CandidateNames.Length == 0)
        {
            return candidates;
        }

        var names = options.CandidateNames.ToHashSet(StringComparer.Ordinal);
        return candidates.Where(candidate => names.Contains(candidate.Name)).ToArray();
    }
}
