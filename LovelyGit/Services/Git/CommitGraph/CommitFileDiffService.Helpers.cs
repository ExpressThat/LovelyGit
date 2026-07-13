using ColorCode;
using ExpressThat.LovelyGit.Services.Data.Repositorys;
using ExpressThat.LovelyGit.Services.Git.CommitGraph.Details;
using ExpressThat.LovelyGit.Services.Git.CommitGraph.Models;
using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser;

namespace ExpressThat.LovelyGit.Services.Git.CommitGraph;

internal sealed partial class CommitFileDiffService : IDisposable
{
    private static string GetStatus(GitTreeFile? oldFile, GitTreeFile? newFile)
    {
        if (oldFile == null)
        {
            return "Added";
        }

        if (newFile == null)
        {
            return "Deleted";
        }

        return oldFile.Mode == newFile.Mode ? "Modified" : "TypeChanged";
    }

    private static ILanguage? ResolveLanguage(string path)
    {
        var extension = Path.GetExtension(path).ToLowerInvariant();
        return extension switch
        {
            ".cs" => Languages.CSharp,
            ".css" => Languages.Css,
            ".fs" or ".fsx" => Languages.FSharp,
            ".htm" or ".html" => Languages.Html,
            ".java" => Languages.Java,
            ".js" or ".jsx" or ".mjs" or ".cjs" => Languages.JavaScript,
            ".json" => Languages.JavaScript,
            ".md" or ".markdown" => Languages.Markdown,
            ".php" => Languages.Php,
            ".ps1" or ".psm1" or ".psd1" => Languages.PowerShell,
            ".py" => Languages.Python,
            ".sql" => Languages.Sql,
            ".ts" or ".tsx" => Languages.Typescript,
            ".vb" => Languages.VbDotNet,
            ".xml" or ".xaml" or ".csproj" or ".slnx" => Languages.Xml,
            _ => null,
        };
    }

    private static string MakeDiffGateKey(
        Guid repositoryId,
        string commitHash,
        string path,
        CommitDiffViewMode viewMode,
        bool ignoreWhitespace)
    {
        return string.Concat(
            repositoryId.ToString("N"),
            ':',
            commitHash,
            ':',
            viewMode.ToString(),
            ':',
            ignoreWhitespace ? "ignore-ws" : "exact",
            ':',
            path);
    }

    private BuildGate GetBuildGate(string key)
    {
        lock (_diffBuildGateLock)
        {
            if (!_diffBuildGates.TryGetValue(key, out var gate))
            {
                gate = new BuildGate();
                _diffBuildGates[key] = gate;
            }

            gate.ReferenceCount++;
            return gate;
        }
    }

    private void ReleaseBuildGate(string key, BuildGate gate)
    {
        lock (_diffBuildGateLock)
        {
            gate.ReferenceCount--;
            if (gate.ReferenceCount == 0
                && _diffBuildGates.TryGetValue(key, out var activeGate)
                && ReferenceEquals(activeGate, gate))
            {
                _diffBuildGates.Remove(key);
                gate.Semaphore.Dispose();
            }
        }
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(CommitFileDiffService));
        }
    }

    private sealed record ActivePreparation(
        string CommitHash,
        CancellationTokenSource CancellationTokenSource,
        Task Task) : IDisposable
    {
        public void Dispose()
        {
            CancellationTokenSource.Dispose();
        }
    }

    private sealed class BuildGate
    {
        public SemaphoreSlim Semaphore { get; } = new(1, 1);

        public int ReferenceCount { get; set; }
    }

    private sealed record CommitFileDiffSource
    {
        public string Status { get; init; } = string.Empty;
        public bool IsBinary { get; init; }
        public string OldText { get; init; } = string.Empty;
        public string NewText { get; init; } = string.Empty;
        public ILanguage? Language { get; init; }
    }
}
