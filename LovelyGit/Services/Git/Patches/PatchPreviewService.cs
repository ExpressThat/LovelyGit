using ExpressThat.LovelyGit.Services.Dialogs;

namespace ExpressThat.LovelyGit.Services.Git.Patches;

internal sealed class PatchPreviewService
{
    private const long MaxPatchBytes = 20 * 1024 * 1024;
    private const int MaxPreviewFiles = 5_000;
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
        await ParseAsync(reader, response, cancellationToken).ConfigureAwait(false);
        if (response.Files.Count == 0)
        {
            throw new InvalidOperationException("The selected file does not contain a unified Git patch.");
        }

        return response;
    }

    private static async Task ParseAsync(
        StreamReader reader,
        PatchPreviewResponse response,
        CancellationToken cancellationToken)
    {
        PatchFilePreview? current = null;
        string? oldPath = null;
        while (await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false) is { } line)
        {
            if (line.StartsWith("--- ", StringComparison.Ordinal))
            {
                oldPath = ParseHeaderPath(line.AsSpan(4));
            }
            else if (line.StartsWith("+++ ", StringComparison.Ordinal))
            {
                current = StartFile(response, ParseHeaderPath(line.AsSpan(4)) ?? oldPath);
            }
            else if (current != null && line.StartsWith('+') && !line.StartsWith("+++"))
            {
                current.Additions++;
                response.TotalAdditions++;
            }
            else if (current != null && line.StartsWith('-') && !line.StartsWith("---"))
            {
                current.Deletions++;
                response.TotalDeletions++;
            }
        }
    }

    private static PatchFilePreview? StartFile(PatchPreviewResponse response, string? path)
    {
        if (path == null) return null;
        if (response.Files.Count >= MaxPreviewFiles)
        {
            response.IsTruncated = true;
            return null;
        }

        var preview = new PatchFilePreview { Path = path };
        response.Files.Add(preview);
        return preview;
    }

    private static string? ParseHeaderPath(ReadOnlySpan<char> value)
    {
        var tabIndex = value.IndexOf('\t');
        if (tabIndex >= 0) value = value[..tabIndex];
        if (value.SequenceEqual("/dev/null")) return null;
        if (value.StartsWith("a/") || value.StartsWith("b/")) value = value[2..];
        return value.Trim().ToString();
    }
}
