namespace LovelyGit.DiffBenchmarks;

internal static class VirtualBillionBenchmarkFixtures
{
    public const string CaseName = "lovelygit-billion-synthetic";
    public const int LineCount = 1_000_000_000;

    public static BenchmarkCase Create()
    {
        return new BenchmarkCase(
            CaseName,
            LineCount,
            string.Empty,
            string.Empty,
            "LovelyGit-only virtual one-object payload count; rows are not materialized.");
    }
}
