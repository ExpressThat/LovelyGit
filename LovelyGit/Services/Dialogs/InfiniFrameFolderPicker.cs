using InfiniFrame;

namespace ExpressThat.LovelyGit.Services.Dialogs;

public class InfiniFrameFolderPicker : IFolderPicker
{
    private readonly InfiniFrameWindowProvider _windowProvider;

    public InfiniFrameFolderPicker(InfiniFrameWindowProvider windowProvider)
    {
        _windowProvider = windowProvider;
    }

    public async Task<string?> PickFolderAsync(
        CancellationToken cancellationToken,
        string title = "Select Git repository")
    {
        var window = _windowProvider.Window
            ?? throw new InvalidOperationException("The application window is not available.");

        var folders = await window
            .ShowOpenFolderAsync(title, string.Empty, false, cancellationToken)
            .ConfigureAwait(false);

        return folders.FirstOrDefault();
    }
}
