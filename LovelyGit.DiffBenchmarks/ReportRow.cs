namespace LovelyGit.DiffBenchmarks;

internal sealed record ReportRow(
    string Candidate,
    string CaseName,
    string ViewMode,
    string Status,
    string Notes,
    int LineCount,
    double DiffMs,
    double SerializeMs,
    long PayloadBytes,
    long MemoryBytes,
    int Rows);
