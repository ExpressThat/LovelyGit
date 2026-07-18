using ExpressThat.LovelyGit.Services.Data;
using ExpressThat.LovelyGit.Services.Data.Models;
using ExpressThat.LovelyGit.Services.Git.CommitGraph.Queries;
using ExpressThat.LovelyGit.Services.Settings;
using BLite.Core.CDC;
using System.Diagnostics;

namespace ExpressThat.LovelyGit.Services.Git.WorkingTree;

internal sealed class ActiveRepositorySettingsWatcher : IHostedService, IDisposable
{
    private readonly AppDbContext _appDbContext;
    private readonly SettingsManager _settingsManager;
    private readonly CommitGraphPageService _commitGraphPageService;
    private readonly WorkingTreeWatcherService _workingTreeWatcherService;
    private IDisposable? _subscription;

    public ActiveRepositorySettingsWatcher(
        AppDbContext appDbContext,
        SettingsManager settingsManager,
        CommitGraphPageService commitGraphPageService,
        WorkingTreeWatcherService workingTreeWatcherService)
    {
        _appDbContext = appDbContext;
        _settingsManager = settingsManager;
        _commitGraphPageService = commitGraphPageService;
        _workingTreeWatcherService = workingTreeWatcherService;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await RefreshActiveRepositoryAsync().ConfigureAwait(false);

        _subscription = _appDbContext.Settings
            .Watch(capturePayload: true)
            .Subscribe(new SettingChangeObserver(this));
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _subscription?.Dispose();
        _subscription = null;
        _workingTreeWatcherService.Dispose();
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _subscription?.Dispose();
    }

    private async Task RefreshActiveRepositoryAsync()
    {
        try
        {
            var repositoryId = await _settingsManager
                .GetSetting(SettingsResolver.CurrentGitRepositoryId)
                .ConfigureAwait(false);
            await _commitGraphPageService
                .SwitchActiveRepositoryAsync(repositoryId)
                .ConfigureAwait(false);
            await _workingTreeWatcherService.SwitchActiveRepositoryAsync(repositoryId).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            Trace.TraceWarning("Repository services failed to follow active repository setting: {0}", exception);
            Console.Error.WriteLine("Repository services failed to follow active repository setting: {0}", exception);
        }
    }

    private sealed class SettingChangeObserver : IObserver<ChangeStreamEvent<string, SettingModel>>
    {
        private readonly ActiveRepositorySettingsWatcher _owner;

        public SettingChangeObserver(ActiveRepositorySettingsWatcher owner)
        {
            _owner = owner;
        }

        public void OnCompleted()
        {
        }

        public void OnError(Exception error)
        {
        }

        public void OnNext(ChangeStreamEvent<string, SettingModel> value)
        {
            if (!string.Equals(value.DocumentId, nameof(Setting.CurrentGitRepositoryId), StringComparison.Ordinal))
            {
                return;
            }

            _ = Task.Run(_owner.RefreshActiveRepositoryAsync, CancellationToken.None);
        }
    }
}
