namespace ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.KnownRepository;

internal sealed class CloneRepositoryProgressPublisher
{
    public event Action<CloneRepositoryProgressNotification>? ProgressChanged;

    public void Publish(CloneRepositoryProgressNotification progress)
    {
        ProgressChanged?.Invoke(progress);
    }
}
