using ExpressThat.LovelyGit.Services.Dialogs;
using ExpressThat.LovelyGit.Services.Git.Cli;
using ExpressThat.LovelyGit.Services.Git.CommitGraph.Models;
using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser;

namespace ExpressThat.LovelyGit.Services.Git.CommitGraph;

internal sealed class CommitArchiveExportService(
    GitCliService git,
    ISaveFilePicker saveFilePicker)
{
    public async Task<CommitArchiveExportResponse> ExportAsync(
        string repositoryPath,
        GitObjectId commitId,
        CancellationToken cancellationToken)
    {
        var shortHash = commitId.ToString()[..12];
        var destination = await saveFilePicker
            .PickSaveFileAsync(
                "Export commit archive",
                $"{shortHash}.zip",
                ["zip", "tar"],
                cancellationToken)
            .ConfigureAwait(false);
        if (destination == null)
        {
            return new CommitArchiveExportResponse();
        }

        var format = Path.GetExtension(destination).Equals(".tar", StringComparison.OrdinalIgnoreCase)
            ? "tar"
            : "zip";
        var directory = Path.GetDirectoryName(Path.GetFullPath(destination))
            ?? throw new InvalidOperationException("The archive destination is invalid.");
        Directory.CreateDirectory(directory);
        var temporaryPath = Path.Combine(directory, $".lovelygit-{Guid.NewGuid():N}.tmp");

        try
        {
            await git.CreateCommand(
                    ["archive", $"--format={format}", $"--output={temporaryPath}", commitId.ToString()],
                    repositoryPath)
                .ExecuteAsync(cancellationToken)
                .ConfigureAwait(false);
            File.Move(temporaryPath, destination, overwrite: true);
            return new CommitArchiveExportResponse { Saved = true, Path = destination };
        }
        finally
        {
            File.Delete(temporaryPath);
        }
    }
}
