
namespace LovelyGit.DiffBenchmarks;

internal static class BenchmarkCandidates
{
    public static IReadOnlyList<BenchmarkCandidate> Create()
    {
        return
        [
            Candidate("DiffPlex", "managed", DiffPlexCandidate.Run),
            Candidate("spkl.Diffs", "managed", SpklDiffCandidate.Run),
            Candidate("MyersDiff", "managed", MyersDiffCandidate.Run),
            Candidate("Diff4Net", "managed/netfx", Diff4NetCandidate.Run),
            Candidate("NGitDiff Myers", "managed/netfx", NGitDiffCandidate.RunMyers),
            Candidate("NGitDiff Histogram", "managed/netfx", NGitDiffCandidate.RunHistogram),
            Candidate("CSharpDiff", "managed", CSharpDiffCandidate.Run),
            Candidate("DiffMatchPatch", "text-sync", DiffMatchPatchCandidate.Run),
            Candidate("Git CLI", "reference/patch-output", GitCliDiffCandidate.Run),
            Candidate("LovelyGit Prototype", "prototype", LovelyGitPrototypeCandidate.Run),
        ];
    }

    private static BenchmarkCandidate Candidate(
        string name,
        string category,
        Func<BenchmarkCase, CommitDiffViewMode, bool, CommitFileDiffResponse> run)
    {
        return new BenchmarkCandidate(name, category, 1_000_000, "full requested sweep", run);
    }
}
