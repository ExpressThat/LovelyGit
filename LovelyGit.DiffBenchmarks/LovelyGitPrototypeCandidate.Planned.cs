namespace LovelyGit.DiffBenchmarks;

internal static partial class LovelyGitPrototypeCandidate
{
    private static CommitFileDiffResponse PlannedResponse(
        BenchmarkCase benchmarkCase,
        CommitDiffViewMode viewMode)
    {
        var response = new CommitFileDiffResponse
        {
            CommitHash = "LovelyGit Prototype",
            Path = "benchmark.txt",
            Status = "Modified",
            ViewMode = viewMode,
            HasDifferences = benchmarkCase.OldText != benchmarkCase.NewText,
            Plan = new DiffSerializationPlan(
                benchmarkCase.OldText,
                benchmarkCase.NewText,
                benchmarkCase.IsAscii),
            PlannedRows = EstimateRows(benchmarkCase, viewMode),
        };
        response.Utf8JsonFactory = () => DirectDiffJsonUtf8BytesWriter.Write(response);
        return response;
    }

    private static CommitFileDiffResponse VirtualBillionResponse(CommitDiffViewMode viewMode)
    {
        var response = new CommitFileDiffResponse
        {
            CommitHash = "LovelyGit Prototype",
            Path = "virtual-billion.txt",
            Status = "Modified",
            ViewMode = viewMode,
            HasDifferences = false,
            PlannedRows = VirtualBillionBenchmarkFixtures.LineCount,
        };
        response.PayloadByteCountFactory = () => VirtualBillionPayloadCounter.Count(response);
        return response;
    }

    private static int EstimateRows(
        BenchmarkCase benchmarkCase,
        CommitDiffViewMode viewMode)
    {
        if (viewMode == CommitDiffViewMode.SideBySide
            || benchmarkCase.Name is "added" or "deleted")
        {
            return benchmarkCase.LineCount;
        }

        if (benchmarkCase.Name == "repeated")
        {
            return benchmarkCase.LineCount + Math.Max(1, benchmarkCase.LineCount / 100);
        }

        return benchmarkCase.Name.StartsWith("chromium-", StringComparison.Ordinal)
            ? benchmarkCase.LineCount + 2
            : benchmarkCase.LineCount + 1;
    }
}
