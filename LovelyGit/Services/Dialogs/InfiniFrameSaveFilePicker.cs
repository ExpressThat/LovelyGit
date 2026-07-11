using InfiniFrame;

namespace ExpressThat.LovelyGit.Services.Dialogs;

internal sealed class InfiniFrameSaveFilePicker : ISaveFilePicker
{
    private readonly InfiniFrameWindowProvider _windowProvider;

    public InfiniFrameSaveFilePicker(InfiniFrameWindowProvider windowProvider)
    {
        _windowProvider = windowProvider;
    }

    public async Task<string?> PickSaveFileAsync(
        string title,
        string suggestedFileName,
        IReadOnlyList<string> extensions,
        CancellationToken cancellationToken)
    {
        var window = _windowProvider.Window
            ?? throw new InvalidOperationException("The application window is not available.");
        var filter = new (string, string[])[]
        {
            (GetFilterLabel(extensions), extensions.ToArray()),
        };
        var selected = await window
            .ShowSaveFileAsync(title, suggestedFileName, filter, cancellationToken)
            .ConfigureAwait(false);
        return string.IsNullOrWhiteSpace(selected) ? null : Path.GetFullPath(selected);
    }

    internal static string GetFilterLabel(IReadOnlyList<string> extensions)
    {
        if (extensions.Contains("patch", StringComparer.OrdinalIgnoreCase))
        {
            return "Patch files";
        }

        return extensions.Any(extension =>
            extension.Equals("zip", StringComparison.OrdinalIgnoreCase) ||
            extension.Equals("tar", StringComparison.OrdinalIgnoreCase))
            ? "Archive files"
            : "Files";
    }
}
