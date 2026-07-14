namespace ExpressThat.LovelyGit.Services.Updates;

internal interface IApplicationUpdateClient
{
    bool IsInstalled { get; }

    bool TryApplyPendingUpdate(string[] restartArguments);

    Task DownloadAvailableUpdateAsync(CancellationToken cancellationToken);
}
