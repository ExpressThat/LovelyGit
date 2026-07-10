using InfiniFrame;

namespace ExpressThat.LovelyGit.Services.Dialogs;

internal sealed class InfiniFrameOpenFilePicker : IOpenFilePicker
{
    private readonly InfiniFrameWindowProvider _windowProvider;

    public InfiniFrameOpenFilePicker(InfiniFrameWindowProvider windowProvider)
    {
        _windowProvider = windowProvider;
    }

    public async Task<string?> PickOpenFileAsync(
        string title,
        IReadOnlyList<string> extensions,
        CancellationToken cancellationToken)
    {
        var window = _windowProvider.Window
            ?? throw new InvalidOperationException("The application window is not available.");
        var selected = await window
            .ShowOpenFileAsync(
                title,
                string.Empty,
                false,
                [("Patch files", extensions.ToArray())],
                cancellationToken)
            .ConfigureAwait(false);
        var path = selected.FirstOrDefault();
        return string.IsNullOrWhiteSpace(path) ? null : Path.GetFullPath(path);
    }
}
