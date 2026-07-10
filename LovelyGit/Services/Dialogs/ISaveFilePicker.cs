namespace ExpressThat.LovelyGit.Services.Dialogs;

public interface ISaveFilePicker
{
    Task<string?> PickSaveFileAsync(
        string title,
        string suggestedFileName,
        IReadOnlyList<string> extensions,
        CancellationToken cancellationToken);
}
