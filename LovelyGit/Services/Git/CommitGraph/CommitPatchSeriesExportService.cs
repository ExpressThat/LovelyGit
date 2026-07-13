using System.Text;
using ExpressThat.LovelyGit.Services.Dialogs;
using ExpressThat.LovelyGit.Services.Git.CommitGraph.Models;
using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser;

namespace ExpressThat.LovelyGit.Services.Git.CommitGraph;

internal sealed class CommitPatchSeriesExportService
{
    private static readonly UTF8Encoding Utf8WithoutBom = new(false);
    private readonly CommitPatchSeriesService _seriesService;
    private readonly ISaveFilePicker _saveFilePicker;

    public CommitPatchSeriesExportService(
        CommitPatchSeriesService seriesService,
        ISaveFilePicker saveFilePicker)
    {
        _seriesService = seriesService;
        _saveFilePicker = saveFilePicker;
    }

    public async Task<CommitPatchExportResponse> ExportAsync(
        string repositoryPath,
        IReadOnlyList<GitObjectId> commitIds,
        CancellationToken cancellationToken)
    {
        var response = await _seriesService.GetAsync(repositoryPath, commitIds, cancellationToken)
            .ConfigureAwait(false);
        CommitPatchSeriesValidation.ThrowIfUnsafe(response);
        var path = await _saveFilePicker.PickSaveFileAsync(
                "Save commit patch series",
                $"{commitIds.Count}-commit-series.patch",
                ["patch", "diff"],
                cancellationToken)
            .ConfigureAwait(false);
        if (path == null) return new CommitPatchExportResponse();

        await File.WriteAllTextAsync(path, response.Patch, Utf8WithoutBom, cancellationToken)
            .ConfigureAwait(false);
        return new CommitPatchExportResponse { Saved = true, Path = path };
    }
}

internal static class CommitPatchSeriesValidation
{
    public static void ThrowIfUnsafe(CommitPatchSeriesResponse response)
    {
        if (response.IsTruncated)
        {
            throw new InvalidOperationException("The patch series is too large to export safely.");
        }

        if (response.HasUnsupportedBinaryChanges)
        {
            throw new InvalidOperationException(
                "The series contains binary changes that cannot yet be exported safely.");
        }
    }
}
