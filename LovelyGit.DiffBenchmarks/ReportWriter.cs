using System.Globalization;
using System.Text;

namespace LovelyGit.DiffBenchmarks;

internal static class ReportWriter
{
    public static void Write(
        string path,
        IReadOnlyList<BenchmarkResult> results,
        IReadOnlyList<BenchmarkCandidate> candidates,
        BenchmarkOptions options)
    {
        var builder = new StringBuilder();
        builder.AppendLine("# Diff Engine Benchmark Report");
        builder.AppendLine();
        builder.AppendLine($"Generated: {DateTimeOffset.Now:O}");
        builder.AppendLine($"Iterations: {options.Iterations}");
        builder.AppendLine(options.TimeoutMs > 0
            ? $"Per-run timeout: {options.TimeoutMs.ToString(CultureInfo.InvariantCulture)} ms"
            : "Per-run timeout: disabled");
        builder.AppendLine($"Synthetic line counts: {string.Join(", ", options.LineCounts.Select(FormatNumber))}");
        builder.AppendLine($"Real Chromium repo: `{options.ChromiumRepoPath}`");
        builder.AppendLine("Real Chromium files are included as additional benchmark rows.");
        builder.AppendLine("Runtime: Native AOT published `win-x64` benchmark binary");
        builder.AppendLine();
        AppendCandidates(builder, candidates);
        AppendDecision(builder);
        AppendSummary(builder, results);
        AppendScenarioTables(builder, results);
        AppendRejected(builder);
        File.WriteAllText(path, builder.ToString());
    }

    private static void AppendCandidates(StringBuilder builder, IReadOnlyList<BenchmarkCandidate> candidates)
    {
        builder.AppendLine("## Candidates");
        builder.AppendLine();
        builder.AppendLine("| Candidate | Category | Max lines | Notes |");
        builder.AppendLine("|---|---:|---:|---|");
        foreach (var candidate in candidates)
        {
            builder.AppendLine($"| {candidate.Name} | {candidate.Category} | {FormatNumber(candidate.MaxLineCount)} | {candidate.Notes} |");
        }

        builder.AppendLine();
    }

    private static void AppendDecision(StringBuilder builder)
    {
        builder.AppendLine("## Implementation Decision");
        builder.AppendLine();
        builder.AppendLine("- Keep DiffPlex as the safe production baseline while replacing large add/delete and simple-edit paths with LovelyGit-owned fast paths.");
        builder.AppendLine("- Do not switch wholesale to Git CLI, DiffMatchPatch, CSharpDiff, Diff4Net, or NGitDiff; they are either reference-only, not Git-style line engines, older netfx packages, or slower on key large/repeated cases.");
        builder.AppendLine("- Continue benchmarking `spkl.Diffs`, `MyersDiff`, and the LovelyGit prototype for the general middle algorithm before replacing the full DiffPlex path.");
        builder.AppendLine("- The measured bottleneck for very large files is no longer only diff algorithm time; JSON payload size and serialization become product-level costs that need streaming or a compact backend-owned model.");
        builder.AppendLine();
    }

    private static void AppendSummary(StringBuilder builder, IReadOnlyList<BenchmarkResult> results)
    {
        builder.AppendLine("## Fastest Measured Result By Scenario");
        builder.AppendLine();
        builder.AppendLine("| Scenario | Lines | View | Candidate | Diff ms | Serialize ms | Payload | Memory | Rows |");
        builder.AppendLine("|---|---:|---|---|---:|---:|---:|---:|---:|");
        var groups = results.Where(result => result.Status == "Measured")
            .GroupBy(result => (result.CaseName, result.LineCount, result.ViewMode))
            .OrderBy(group => group.Key.CaseName)
            .ThenBy(group => group.Key.LineCount)
            .ThenBy(group => group.Key.ViewMode);
        foreach (var group in groups)
        {
            var best = group.MinBy(result => result.DiffMs + result.SerializeMs)!;
            AppendResultRow(builder, best);
        }

        builder.AppendLine();
    }

    private static void AppendScenarioTables(StringBuilder builder, IReadOnlyList<BenchmarkResult> results)
    {
        foreach (var scenario in results.Select(result => result.CaseName).Distinct().Order())
        {
            builder.AppendLine($"## Scenario: {scenario}");
            builder.AppendLine();
            builder.AppendLine("| Candidate | Lines | View | Status | Diff ms | Serialize ms | Payload | Memory | Rows | Notes |");
            builder.AppendLine("|---|---:|---|---|---:|---:|---:|---:|---:|---|");
            foreach (var result in results.Where(result => result.CaseName == scenario)
                         .OrderBy(result => result.Candidate)
                         .ThenBy(result => result.LineCount)
                         .ThenBy(result => result.ViewMode))
            {
                builder.AppendLine(
                    $"| {result.Candidate} | {FormatNumber(result.LineCount)} | {result.ViewMode} | {result.Status} | {FormatMs(result.DiffMs)} | {FormatMs(result.SerializeMs)} | {FormatBytes(result.PayloadBytes)} | {FormatBytes(result.MemoryBytes)} | {FormatNumber(result.Rows)} | {result.Notes} |");
            }

            builder.AppendLine();
        }
    }

    private static void AppendRejected(StringBuilder builder)
    {
        builder.AppendLine("## Rejected Or Reference-Only Candidates");
        builder.AppendLine();
        builder.AppendLine("| Candidate | Reason |");
        builder.AppendLine("|---|---|");
        builder.AppendLine("| TextDiff.Sharp | Applies/processes unified diffs; docs recommend DiffPlex for generation. |");
        builder.AppendLine("| ParseDiff | Unified diff parser, not a primary generator. |");
        builder.AppendLine("| LibGit2Sharp | Native dependency and read-path policy risk; keep as future reference only. |");
        builder.AppendLine("| google-diff-match-patch | Character/text sync API; not Git-style line hunk output. |");
        builder.AppendLine();
    }

    private static void AppendResultRow(StringBuilder builder, BenchmarkResult best)
    {
        builder.AppendLine(
            $"| {best.CaseName} | {FormatNumber(best.LineCount)} | {best.ViewMode} | {best.Candidate} | {FormatMs(best.DiffMs)} | {FormatMs(best.SerializeMs)} | {FormatBytes(best.PayloadBytes)} | {FormatBytes(best.MemoryBytes)} | {FormatNumber(best.Rows)} |");
    }

    private static string FormatMs(double value)
    {
        return value == 0 ? "" : value.ToString("0.###", CultureInfo.InvariantCulture);
    }

    private static string FormatNumber(int value)
    {
        return value.ToString("N0", CultureInfo.InvariantCulture);
    }

    private static string FormatBytes(long value)
    {
        return value == 0 ? "" : value.ToString("N0", CultureInfo.InvariantCulture);
    }
}
