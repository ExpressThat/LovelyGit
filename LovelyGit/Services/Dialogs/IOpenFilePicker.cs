namespace ExpressThat.LovelyGit.Services.Dialogs;

public interface IOpenFilePicker
{
    Task<string?> PickOpenFileAsync(
        string title,
        IReadOnlyList<string> extensions,
        CancellationToken cancellationToken);
}
