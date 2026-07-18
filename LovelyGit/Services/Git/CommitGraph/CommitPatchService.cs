using System.Text;
using ExpressThat.LovelyGit.Services.Git.CommitGraph.Details;
using ExpressThat.LovelyGit.Services.Git.CommitGraph.Models;
using ExpressThat.LovelyGit.Services.Git.Diffing;
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
        using var repository = await LovelyGitRepository
            .OpenObjectDatabaseAsync(repositoryPath, cancellationToken)
            .ConfigureAwait(false);
        return await GetCommitPatchAsync(repository, commitId, cancellationToken).ConfigureAwait(false);
    }

    internal async Task<CommitPatchResponse> GetCommitPatchAsync(
        LovelyGitRepository repository,
        GitObjectId commitId,
        CancellationToken cancellationToken)
    {
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

        if (changedPaths.Count > MaxFiles)
        {
            return Truncated(commit);
        }

        var truncated = false;
        var hasUnsupportedBinaryChanges = false;
        foreach (var path in changedPaths)
        {
            var result = await AppendFilePatchAsync(
                    patch,
                    path,
                    comparison.ParentFiles.GetValueOrDefault(path),
                    comparison.CurrentFiles.GetValueOrDefault(path),
                    analyzer,
                    cancellationToken)
                .ConfigureAwait(false);
            hasUnsupportedBinaryChanges |= result.HasUnsupportedBinaryChanges;

            if (result.IsTruncated)
            {
                truncated = true;
                break;
            }
        }

        return new CommitPatchResponse
        {
            CommitHash = commit.Hash.ToString(),
            Patch = truncated ? string.Empty : NormalizeNewLines(patch.ToString()),
            IsTruncated = truncated,
            HasUnsupportedBinaryChanges = hasUnsupportedBinaryChanges,
        };
    }

    private static async Task<FilePatchResult> AppendFilePatchAsync(
        StringBuilder patch,
        string path,
        GitTreeFile? oldFile,
        GitTreeFile? newFile,
        BlobLineAnalyzer analyzer,
        CancellationToken cancellationToken)
    {
        patch.Append("diff --git a/").Append(path).Append(" b/").AppendLine(path);
        AppendModeChange(patch, oldFile, newFile);

        var oldBlob = await ReadBlobTextAsync(analyzer, oldFile, cancellationToken).ConfigureAwait(false);
        var newBlob = await ReadBlobTextAsync(analyzer, newFile, cancellationToken).ConfigureAwait(false);
        if (oldBlob.IsBinary || newBlob.IsBinary)
        {
            patch.Append("Binary files a/")
                .Append(path)
                .Append(" and b/")
                .Append(path)
                .AppendLine(" differ");
            return new(true, false);
        }

        var rendered = UnifiedDiffRenderer.TryRender(
            oldBlob.Text,
            newBlob.Text,
            oldFile is null ? "/dev/null" : "a/" + path,
            newFile is null ? "/dev/null" : "b/" + path,
            ContextLines,
            Math.Max(0, MaxPatchCharacters - patch.Length),
            out var unified);
        if (!rendered) return new(false, true);
        patch.Append(NormalizeNewLines(unified).TrimEnd('\n')).AppendLine();
        return new(false, false);
    }

    private static CommitPatchResponse Truncated(GitCommit commit) => new()
    {
        CommitHash = commit.Hash.ToString(),
        IsTruncated = true,
    };

    private static void AppendModeChange(
        StringBuilder patch,
        GitTreeFile? oldFile,
        GitTreeFile? newFile)
    {
        if (oldFile is null)
        {
            AppendModeLine(patch, "new file mode", newFile);
            return;
        }
        if (newFile is null)
        {
            AppendModeLine(patch, "deleted file mode", oldFile);
            return;
        }
        if (oldFile.Mode == newFile.Mode) return;
        AppendModeLine(patch, "old mode", oldFile);
        AppendModeLine(patch, "new mode", newFile);
    }

    private static void AppendModeLine(StringBuilder patch, string label, GitTreeFile? file)
    {
        if (file != null)
        {
            patch.Append(label).Append(' ').AppendLine(file.Mode);
        }
    }

    private static ValueTask<BlobText> ReadBlobTextAsync(
        BlobLineAnalyzer analyzer,
        GitTreeFile? file,
        CancellationToken cancellationToken)
    {
        return file == null
            ? ValueTask.FromResult(new BlobText(false, string.Empty))
            : analyzer.ReadTextAsync(file, cancellationToken);
    }

    private static string NormalizeNewLines(string value)
    {
        return value.Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace('\r', '\n');
    }

    private readonly record struct FilePatchResult(
        bool HasUnsupportedBinaryChanges,
        bool IsTruncated);
}
