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
            ("Patch files", extensions.ToArray()),
        };
        var selected = await window
            .ShowSaveFileAsync(title, suggestedFileName, filter, cancellationToken)
            .ConfigureAwait(false);
        return string.IsNullOrWhiteSpace(selected) ? null : Path.GetFullPath(selected);
    }
}
