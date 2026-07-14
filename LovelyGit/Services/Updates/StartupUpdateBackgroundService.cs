namespace ExpressThat.LovelyGit.Services.Updates;

internal sealed class StartupUpdateBackgroundService(
    StartupUpdateCoordinator coordinator,
    IHostApplicationLifetime applicationLifetime) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await WaitForApplicationStartedAsync(applicationLifetime, stoppingToken).ConfigureAwait(false);
        await coordinator.DownloadAvailableUpdateAsync(stoppingToken).ConfigureAwait(false);
    }

    private static async Task WaitForApplicationStartedAsync(
        IHostApplicationLifetime applicationLifetime,
        CancellationToken stoppingToken)
    {
        if (applicationLifetime.ApplicationStarted.IsCancellationRequested)
        {
            return;
        }

        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var startedRegistration = applicationLifetime.ApplicationStarted.Register(
            () => started.TrySetResult());
        using var stoppingRegistration = stoppingToken.Register(
            () => started.TrySetCanceled(stoppingToken));
        await started.Task.ConfigureAwait(false);
    }
}
