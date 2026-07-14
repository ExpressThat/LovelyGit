using Velopack;
using Velopack.Sources;

namespace ExpressThat.LovelyGit.Services.Updates;

internal sealed class VelopackUpdateClient : IApplicationUpdateClient
{
    private const string GitHubRepositoryUrl = "https://github.com/ExpressThat/LovelyGit";
    private const bool IncludePrereleases = true;
    private readonly UpdateManager _updateManager;

    public VelopackUpdateClient()
    {
        var source = new GithubSource(
            GitHubRepositoryUrl,
            accessToken: null,
            prerelease: IncludePrereleases,
            downloader: null);
        _updateManager = new UpdateManager(source);
    }

    public bool IsInstalled => _updateManager.IsInstalled;

    public bool TryApplyPendingUpdate(string[] restartArguments)
    {
        var pendingUpdate = _updateManager.UpdatePendingRestart;
        if (pendingUpdate is null)
        {
            return false;
        }

        _updateManager.ApplyUpdatesAndRestart(pendingUpdate, restartArguments);
        return true;
    }

    public async Task DownloadAvailableUpdateAsync(CancellationToken cancellationToken)
    {
        var update = await _updateManager
            .CheckForUpdatesAsync()
            .WaitAsync(cancellationToken)
            .ConfigureAwait(false);
        if (update is null)
        {
            return;
        }

        await _updateManager
            .DownloadUpdatesAsync(update, progress: null, cancellationToken)
            .ConfigureAwait(false);
    }
}
