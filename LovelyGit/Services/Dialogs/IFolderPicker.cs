namespace ExpressThat.LovelyGit.Services.Dialogs;

public interface IFolderPicker
{
    Task<string?> PickFolderAsync(CancellationToken cancellationToken);
}
