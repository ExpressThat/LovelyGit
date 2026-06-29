using System.Diagnostics;

namespace LovelyGit.DiffBenchmarks;

internal static class GitCliDiffCandidate
{
    public static CommitFileDiffResponse Run(
        BenchmarkCase benchmarkCase,
        CommitDiffViewMode viewMode,
        bool ignoreWhitespace)
    {
        var directory = Directory.CreateTempSubdirectory("lovelygit-diff-cli-");
        try
        {
            var oldPath = Path.Combine(directory.FullName, "old.txt");
            var newPath = Path.Combine(directory.FullName, "new.txt");
            File.WriteAllText(oldPath, benchmarkCase.OldText);
            File.WriteAllText(newPath, benchmarkCase.NewText);
            var args = ignoreWhitespace ? "diff --no-index -w -- old.txt new.txt" : "diff --no-index -- old.txt new.txt";
            var output = RunGit(directory.FullName, args);
            var rows = output.Split('\n').Count(line => line.StartsWith('+') || line.StartsWith('-') || line.StartsWith(' '));
            var lines = Enumerable.Range(0, rows)
                .Select(index => new DiffOperation("Modified", index + 1, index + 1, string.Empty, string.Empty))
                .ToList();
            return DiffResponseFactory.FromOperations("Git CLI", viewMode, lines);
        }
        finally
        {
            directory.Delete(recursive: true);
        }
    }

    private static string RunGit(string workingDirectory, string arguments)
    {
        using var process = Process.Start(new ProcessStartInfo("git", arguments)
        {
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        });
        var output = process?.StandardOutput.ReadToEnd() ?? string.Empty;
        process?.StandardError.ReadToEnd();
        process?.WaitForExit();
        return output;
    }
}
