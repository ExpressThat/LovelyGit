using System.Text;
using DiffPlex;
using DiffPlex.Renderer;
using ExpressThat.LovelyGit.Services.Git.CommitGraph.Details;
using ExpressThat.LovelyGit.Services.Git.CommitGraph.Models;
using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser;

namespace ExpressThat.LovelyGit.Services.Git.CommitGraph;

internal sealed class CommitPatchService
{
    private const int ContextLines = 3;
    private const int MaxFiles = 200;
    private const int MaxPatchCharacters = 2_000_000;

    public async Task<CommitPatchResponse> GetCommitPatchAsync(
        string repositoryPath,
        GitObjectId commitId,
        CancellationToken cancellationToken)
    {
        using var repository = await LovelyGitRepository.OpenAsync(repositoryPath, cancellationToken)
            .ConfigureAwait(false);
        var commit = await repository.GetCommitAsync(commitId, cancellationToken).ConfigureAwait(false);
        GitCommit? firstParent = null;
        if (commit.ParentHashes.Count > 0)
        {
            firstParent = await repository.GetCommitAsync(commit.ParentHashes[0], cancellationToken)
                .ConfigureAwait(false);
        }

        var comparison = await repository
            .GetChangedTreeFilesAsync(firstParent?.TreeHash, commit.TreeHash, cancellationToken)
            .ConfigureAwait(false);
        var analyzer = new BlobLineAnalyzer(repository);
        var patch = new StringBuilder();
        var changedPaths = comparison.CurrentFiles.Keys
            .Concat(comparison.ParentFiles.Keys)
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .Take(MaxFiles + 1)
            .ToList();

        var truncated = changedPaths.Count > MaxFiles;
        foreach (var path in changedPaths.Take(MaxFiles))
        {
            await AppendFilePatchAsync(
                    patch,
                    path,
                    comparison.ParentFiles.GetValueOrDefault(path),
                    comparison.CurrentFiles.GetValueOrDefault(path),
                    analyzer,
                    cancellationToken)
                .ConfigureAwait(false);

            if (patch.Length > MaxPatchCharacters)
            {
                truncated = true;
                break;
            }
        }

        if (truncated)
        {
            patch.AppendLine();
            patch.AppendLine("# Patch truncated by LovelyGit.");
        }

        return new CommitPatchResponse
        {
            CommitHash = commit.Hash.ToString(),
            Patch = patch.ToString(),
            IsTruncated = truncated,
        };
    }

    private static async Task AppendFilePatchAsync(
        StringBuilder patch,
        string path,
        GitTreeFile? oldFile,
        GitTreeFile? newFile,
        BlobLineAnalyzer analyzer,
        CancellationToken cancellationToken)
    {
        patch.Append("diff --git a/").Append(path).Append(" b/").AppendLine(path);
        if (oldFile?.Mode != newFile?.Mode)
        {
            AppendModeLine(patch, "old mode", oldFile);
            AppendModeLine(patch, "new mode", newFile);
        }

        var oldBlob = await ReadBlobTextAsync(analyzer, oldFile, cancellationToken).ConfigureAwait(false);
        var newBlob = await ReadBlobTextAsync(analyzer, newFile, cancellationToken).ConfigureAwait(false);
        if (oldBlob.IsBinary || newBlob.IsBinary)
        {
            patch.Append("Binary files a/")
                .Append(path)
                .Append(" and b/")
                .Append(path)
                .AppendLine(" differ");
            return;
        }

        var unified = UnidiffRenderer.GenerateUnidiff(
            oldBlob.Text,
            newBlob.Text,
            "a/" + path,
            "b/" + path,
            ignoreWhitespace: false,
            ignoreCase: false,
            ContextLines);
        patch.Append(NormalizeNewLines(unified).TrimEnd()).AppendLine();
    }

    private static void AppendModeLine(StringBuilder patch, string label, GitTreeFile? file)
    {
        if (file != null)
        {
            patch.Append(label).Append(' ').AppendLine(file.Mode);
        }
    }

    private static Task<BlobText> ReadBlobTextAsync(
        BlobLineAnalyzer analyzer,
        GitTreeFile? file,
        CancellationToken cancellationToken)
    {
        return file == null
            ? Task.FromResult(new BlobText(false, string.Empty))
            : analyzer.ReadTextAsync(file, cancellationToken);
    }

    private static string NormalizeNewLines(string value)
    {
        return value.Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace('\r', '\n');
    }
}
