using System.Text;
using ExpressThat.LovelyGit.Services.Dialogs;
using ExpressThat.LovelyGit.Services.Git.CommitGraph.Models;
using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser;

namespace ExpressThat.LovelyGit.Services.Git.CommitGraph;

internal sealed class CommitPatchExportService
{
    private static readonly UTF8Encoding Utf8WithoutBom = new(false);
    private readonly CommitPatchService _patchService;
    private readonly ISaveFilePicker _saveFilePicker;

    public CommitPatchExportService(
        CommitPatchService patchService,
        ISaveFilePicker saveFilePicker)
    {
        _patchService = patchService;
        _saveFilePicker = saveFilePicker;
    }

    public async Task<CommitPatchExportResponse> ExportAsync(
        string repositoryPath,
        GitObjectId commitId,
        CancellationToken cancellationToken)
    {
        var response = await _patchService
            .GetCommitPatchAsync(repositoryPath, commitId, cancellationToken)
            .ConfigureAwait(false);
        if (response.IsTruncated)
        {
            throw new InvalidOperationException("The patch is too large to save safely.");
        }

        if (response.HasUnsupportedBinaryChanges)
        {
            throw new InvalidOperationException(
                "This commit contains binary changes that cannot yet be exported safely.");
        }

        var shortHash = response.CommitHash[..Math.Min(12, response.CommitHash.Length)];
        var path = await _saveFilePicker
            .PickSaveFileAsync(
                "Save commit patch",
                $"{shortHash}.patch",
                ["patch", "diff"],
                cancellationToken)
            .ConfigureAwait(false);
        if (path == null)
        {
            return new CommitPatchExportResponse();
        }

        await File.WriteAllTextAsync(path, response.Patch, Utf8WithoutBom, cancellationToken)
            .ConfigureAwait(false);
        return new CommitPatchExportResponse { Saved = true, Path = path };
    }
}
