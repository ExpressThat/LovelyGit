namespace LovelyGit.DiffBenchmarks;

internal sealed record BenchmarkOptions(
    int[] LineCounts,
    int Iterations,
    int TimeoutMs,
    string ArtifactDirectory,
    string ChromiumRepoPath,
    string[] CandidateNames,
    long MemoryLimitBytes,
    int SlowSkipThresholdMs,
    string? ChildCandidate,
    string? ChildCase,
    int? ChildLineCount,
    string? ChildViewMode,
    string? ChildResultPath)
{
    public bool IsChildRun => ChildCandidate is not null;

    public static BenchmarkOptions Parse(string[] args)
    {
        var lineCounts = new[] { 10, 100, 1_000, 10_000, 100_000, 1_000_000 };
        var iterations = 1;
        var timeoutMs = 5_000;
        var artifacts = Path.GetFullPath("artifacts");
        var chromiumRepoPath = @"C:\Projects\chromium-tessting";
        var candidateNames = Array.Empty<string>();
        var memoryLimitBytes = 4L * 1024 * 1024 * 1024;
        var slowSkipThresholdMs = 1_000;
        string? childCandidate = null;
        string? childCase = null;
        int? childLineCount = null;
        string? childViewMode = null;
        string? childResultPath = null;

        for (var index = 0; index < args.Length; index++)
        {
            if (args[index] == "--lines" && index + 1 < args.Length)
            {
                lineCounts = args[++index].Split(',', StringSplitOptions.TrimEntries)
                    .Select(int.Parse)
                    .ToArray();
            }
            else if (args[index] == "--iterations" && index + 1 < args.Length)
            {
                iterations = int.Parse(args[++index]);
            }
            else if (args[index] == "--timeout-ms" && index + 1 < args.Length)
            {
                timeoutMs = int.Parse(args[++index]);
            }
            else if (args[index] == "--artifacts" && index + 1 < args.Length)
            {
                artifacts = Path.GetFullPath(args[++index]);
            }
            else if (args[index] == "--chromium-repo" && index + 1 < args.Length)
            {
                chromiumRepoPath = Path.GetFullPath(args[++index]);
            }
            else if (args[index] == "--candidates" && index + 1 < args.Length)
            {
                candidateNames = args[++index].Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            }
            else if (args[index] == "--memory-limit-gb" && index + 1 < args.Length)
            {
                memoryLimitBytes = (long)(double.Parse(args[++index]) * 1024 * 1024 * 1024);
            }
            else if (args[index] == "--slow-skip-ms" && index + 1 < args.Length)
            {
                slowSkipThresholdMs = int.Parse(args[++index]);
            }
            else if (args[index] == "--child-candidate" && index + 1 < args.Length)
            {
                childCandidate = args[++index];
            }
            else if (args[index] == "--child-case" && index + 1 < args.Length)
            {
                childCase = args[++index];
            }
            else if (args[index] == "--child-line-count" && index + 1 < args.Length)
            {
                childLineCount = int.Parse(args[++index]);
            }
            else if (args[index] == "--child-view-mode" && index + 1 < args.Length)
            {
                childViewMode = args[++index];
            }
            else if (args[index] == "--child-result" && index + 1 < args.Length)
            {
                childResultPath = args[++index];
            }
        }

        return new BenchmarkOptions(
            lineCounts,
            iterations,
            timeoutMs,
            artifacts,
            chromiumRepoPath,
            candidateNames,
            memoryLimitBytes,
            slowSkipThresholdMs,
            childCandidate,
            childCase,
            childLineCount,
            childViewMode,
            childResultPath);
    }
}
