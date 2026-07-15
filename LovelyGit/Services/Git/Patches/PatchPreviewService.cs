using ExpressThat.LovelyGit.Services.Dialogs;

namespace ExpressThat.LovelyGit.Services.Git.Patches;

internal sealed class PatchPreviewService
{
    private const long MaxPatchBytes = 20 * 1024 * 1024;
    private readonly IOpenFilePicker _filePicker;

    public PatchPreviewService(IOpenFilePicker filePicker)
    {
        _filePicker = filePicker;
    }

    public async Task<PatchPreviewResponse> ChooseAndReadAsync(CancellationToken cancellationToken)
    {
        var path = await _filePicker
            .PickOpenFileAsync("Choose patch to apply", ["patch", "diff"], cancellationToken)
            .ConfigureAwait(false);
        return path == null
            ? new PatchPreviewResponse()
            : await ReadAsync(path, cancellationToken).ConfigureAwait(false);
    }

    internal static async Task<PatchPreviewResponse> ReadAsync(
        string path,
        CancellationToken cancellationToken)
    {
        var file = new FileInfo(path);
        if (!file.Exists)
        {
            throw new FileNotFoundException("The selected patch no longer exists.", path);
        }

        if (file.Length > MaxPatchBytes)
        {
            throw new InvalidOperationException("Patch files larger than 20 MB are not supported.");
        }

        var response = new PatchPreviewResponse
        {
            Selected = true,
            Path = file.FullName,
            FileName = file.Name,
        };
        using var stream = new FileStream(
            file.FullName,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 16 * 1024,
            FileOptions.Asynchronous | FileOptions.SequentialScan);
        using var reader = new StreamReader(stream, detectEncodingFromByteOrderMarks: true);
        await PatchPreviewParser.ParseAsync(reader, response, cancellationToken).ConfigureAwait(false);
        if (response.Files.Count == 0)
        {
            throw new InvalidOperationException("The selected file does not contain a unified Git patch.");
        }

        return response;
    }

}
