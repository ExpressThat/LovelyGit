using ExpressThat.LovelyGit.Services.Git.FileHistory;
using ExpressThat.LovelyGit.Services.NativeMessaging;
using ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.CommitGraph;
using ExpressThat.LovelyGit.Services.NativeMessaging.Commands;

namespace LovelyGit.Tests.Git.FileHistory;

public sealed class FileHistoryServiceTests
{
    [Fact]
    public async Task Cancel_StopsActiveReadAndAllowsRetry()
    {
        var repositoryId = Guid.NewGuid();
        var started = new TaskCompletionSource(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var calls = 0;
        using var service = new FileHistoryService(ReadAsync);

        var first = service.ReadAsync(repositoryId, "repo", "file.txt", null, 100, true);
        await started.Task.WaitAsync(TimeSpan.FromSeconds(2));
        service.Cancel(repositoryId);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => first);
        var retry = await service.ReadAsync(
            repositoryId, "repo", "file.txt", null, 100, false);

        Assert.Equal("file.txt", retry.Path);
        Assert.Equal(2, calls);
        return;

        async Task<FileHistoryResponse> ReadAsync(
            string repositoryPath,
            string path,
            string? startCommitHash,
            int limit,
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

            return new FileHistoryResponse { Path = path };
        }
    }

    [Fact]
    public void Cancel_WithoutActiveReadIsSafe()
    {
        using var service = new FileHistoryService((_, _, _, _, _, _, _) =>
            Task.FromResult(new FileHistoryResponse()));

        service.Cancel(Guid.NewGuid());
    }

    [Fact]
    public async Task CancelCommand_RejectsMissingRepositoryId()
    {
        using var service = PassiveService();
        var resolver = new CancelFileHistoryCommandResolver(service);

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
        using var service = new FileHistoryService(async (_, _, _, _, _, _, token) =>
        {
            started.TrySetResult();
            await Task.Delay(Timeout.InfiniteTimeSpan, token);
            return new FileHistoryResponse();
        });
        var resolver = new CancelFileHistoryCommandResolver(service);
        var read = service.ReadAsync(repositoryId, "repo", "file.txt", null, 100, true);
        await started.Task.WaitAsync(TimeSpan.FromSeconds(2));

        var response = await resolver.Resolve(CancelCommand(repositoryId));

        Assert.True(response.IsSuccess);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => read);
    }

    private static FileHistoryService PassiveService() => new(
        (_, _, _, _, _, _, _) => Task.FromResult(new FileHistoryResponse()));

    private static NativeCommand<CancelFileHistoryCommandArguments> CancelCommand(Guid id) =>
        new()
        {
            Arguments = new CancelFileHistoryCommandArguments { KnownRepositoryId = id },
            CommandType = NativeMessageType.CancelFileHistory,
            CommandUniqueId = "cancel-history",
        };
}
