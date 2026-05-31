using InfiniFrame;

namespace ExpressThat.LovelyGit.Services.Dialogs;

public class InfiniFrameFolderPicker : IFolderPicker
{
    private readonly InfiniFrameWindowProvider _windowProvider;

    public InfiniFrameFolderPicker(InfiniFrameWindowProvider windowProvider)
    {
        _windowProvider = windowProvider;
    }

    public async Task<string?> PickFolderAsync(CancellationToken cancellationToken)
    {
        var window = _windowProvider.Window
            ?? throw new InvalidOperationException("The application window is not available.");

        var folders = await window
            .ShowOpenFolderAsync("Select Git repository", string.Empty, false, cancellationToken)
            .ConfigureAwait(false);

        return folders.FirstOrDefault();
    }
}
