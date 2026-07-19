using ExpressThat.LovelyGit.Services.Git.FileBlame;
using ExpressThat.LovelyGit.Services.NativeMessaging;
using ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.CommitGraph;
using ExpressThat.LovelyGit.Services.NativeMessaging.Commands;

namespace LovelyGit.Tests.Git.FileBlame;

public sealed class FileBlameServiceTests
{
    [Fact]
    public async Task Cancel_StopsActiveReadAndAllowsRetry()
    {
        var repositoryId = Guid.NewGuid();
        var started = new TaskCompletionSource(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var calls = 0;
        using var service = new FileBlameService(ReadAsync);

        var first = service.ReadAsync(repositoryId, "repo", "file.txt", null, true);
        await started.Task.WaitAsync(TimeSpan.FromSeconds(2));
        service.Cancel(repositoryId);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => first);
        var retry = await service.ReadAsync(repositoryId, "repo", "file.txt", null, false);

        Assert.Equal("file.txt", retry.Path);
        Assert.Equal(2, calls);
        return;

        async Task<FileBlameResponse> ReadAsync(
            string repositoryPath,
            string path,
            string? startCommitHash,
            int maximumCommits,
            TimeSpan maximumDuration,
            CancellationToken cancellationToken)
        {
            calls++;
            if (calls == 1)
            {
                started.TrySetResult();
                await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            }

            return new FileBlameResponse { Path = path };
        }
    }

    [Fact]
    public void Cancel_WithoutActiveReadIsSafe()
    {
        using var service = PassiveService();

        service.Cancel(Guid.NewGuid());
    }

    [Fact]
    public async Task CancelCommand_RejectsMissingRepositoryId()
    {
        using var service = PassiveService();
        var resolver = new CancelFileBlameCommandResolver(service);

        var response = await resolver.Resolve(CancelCommand(Guid.Empty));

        Assert.False(response.IsSuccess);
        Assert.Equal("KnownRepositoryId is required.", response.ErrorMessage);
    }

    [Fact]
    public async Task CancelCommand_StopsRepositoryRead()
    {
        var repositoryId = Guid.NewGuid();
        var started = new TaskCompletionSource(
            TaskCreationOptions.RunContinuationsAsynchronously);
        using var service = new FileBlameService(async (_, _, _, _, _, token) =>
        {
            started.TrySetResult();
            await Task.Delay(Timeout.InfiniteTimeSpan, token);
            return new FileBlameResponse();
        });
        var resolver = new CancelFileBlameCommandResolver(service);
        var read = service.ReadAsync(repositoryId, "repo", "file.txt", null, true);
        await started.Task.WaitAsync(TimeSpan.FromSeconds(2));

        var response = await resolver.Resolve(CancelCommand(repositoryId));

        Assert.True(response.IsSuccess);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => read);
    }

    private static FileBlameService PassiveService() => new(
        (_, _, _, _, _, _) => Task.FromResult(new FileBlameResponse()));

    private static NativeCommand<CancelFileBlameCommandArguments> CancelCommand(Guid id) =>
        new()
        {
            Arguments = new CancelFileBlameCommandArguments { KnownRepositoryId = id },
            CommandType = NativeMessageType.CancelFileBlame,
            CommandUniqueId = "cancel-blame",
        };
}
