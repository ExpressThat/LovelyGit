using ExpressThat.LovelyGit.Services.Git.Cli;
using ExpressThat.LovelyGit.Services.Git.Reset;
using ExpressThat.LovelyGit.Services.Git.WorkingTree;
using LovelyGit.Tests.Git.Branches;

namespace LovelyGit.Tests.Git.WorkingTree;

public sealed class UndoLastCommitServiceTests
{
    [Fact]
    public async Task UndoAsync_SoftResetsAndPreservesIndexAndWorktreeChanges()
    {
        using var repository = TemporaryGitRepository.Create();
        await WriteCommitAsync(repository, "undo.txt", "committed", "Undo me", "Original body");
        var undoneHash = await HeadAsync(repository);
        await File.WriteAllTextAsync(Path.Combine(repository.Path, "later.txt"), "staged later");
        await GitAsync(repository, "add", "later.txt");
        await File.WriteAllTextAsync(Path.Combine(repository.Path, "undo.txt"), "unstaged later");

        var result = await CreateService(repository).UndoAsync(
            repository.Path, undoneHash, CancellationToken.None);

        Assert.Equal(repository.HeadCommitHash, await HeadAsync(repository));
        Assert.Equal("Undo me", result.Title);
        Assert.Equal("Original body", result.Body);
        Assert.Equal(
            ["later.txt", "undo.txt"],
            Lines(await GitOutputAsync(repository, "diff", "--cached", "--name-only")));
        Assert.Equal(["undo.txt"], Lines(await GitOutputAsync(repository, "diff", "--name-only")));
        Assert.Equal("unstaged later", await File.ReadAllTextAsync(Path.Combine(repository.Path, "undo.txt")));
    }

    [Fact]
    public async Task UndoAsync_RejectsInitialCommitWithoutMutation()
    {
        using var repository = TemporaryGitRepository.Create();
        var state = await CaptureAsync(repository);

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            CreateService(repository).UndoAsync(
                repository.Path, repository.HeadCommitHash, CancellationToken.None));

        Assert.Contains("initial commit", error.Message, StringComparison.OrdinalIgnoreCase);
        await AssertUnchangedAsync(repository, state);
    }

    [Fact]
    public async Task UndoAsync_RejectsStaleHeadWithoutMutation()
    {
        using var repository = TemporaryGitRepository.Create();
        await WriteCommitAsync(repository, "new.txt", "new", "New head");
        var state = await CaptureAsync(repository);

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            CreateService(repository).UndoAsync(
                repository.Path, repository.HeadCommitHash, CancellationToken.None));

        Assert.Contains("HEAD changed", error.Message, StringComparison.Ordinal);
        await AssertUnchangedAsync(repository, state);
    }

    [Fact]
    public async Task UndoAsync_RejectsDetachedHeadAndActiveOperationWithoutMutation()
    {
        using var repository = TemporaryGitRepository.Create();
        await WriteCommitAsync(repository, "new.txt", "new", "New head");
        var head = await HeadAsync(repository);
        await GitAsync(repository, "checkout", "--detach", head);
        var detached = await CaptureAsync(repository);

        var detachedError = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            CreateService(repository).UndoAsync(repository.Path, head, CancellationToken.None));
        Assert.Contains("local branch", detachedError.Message, StringComparison.OrdinalIgnoreCase);
        await AssertUnchangedAsync(repository, detached);

        await GitAsync(repository, "switch", "-");
        await File.WriteAllTextAsync(Path.Combine(repository.Path, ".git", "MERGE_HEAD"), repository.HeadCommitHash);
        var operation = await CaptureAsync(repository);
        var operationError = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            CreateService(repository).UndoAsync(repository.Path, head, CancellationToken.None));
        Assert.Contains("active merge", operationError.Message, StringComparison.OrdinalIgnoreCase);
        await AssertUnchangedAsync(repository, operation);
    }

    [Fact]
    public async Task UndoAsync_CancellationDoesNotMutateRepository()
    {
        using var repository = TemporaryGitRepository.Create();
        await WriteCommitAsync(repository, "new.txt", "new", "New head");
        var state = await CaptureAsync(repository);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            CreateService(repository).UndoAsync(repository.Path, state.Head, cancellation.Token));
        await AssertUnchangedAsync(repository, state);
    }

    private static UndoLastCommitService CreateService(TemporaryGitRepository repository) =>
        new(
            new HeadCommitMessageService(),
            new GitResetCommandService(new GitOperationService(repository.GitCliService)));

    private static async Task WriteCommitAsync(
        TemporaryGitRepository repository,
        string path,
        string content,
        string title,
        string? body = null)
    {
        await File.WriteAllTextAsync(Path.Combine(repository.Path, path), content);
        await GitAsync(repository, "add", path);
        var arguments = body is null
            ? new[] { "commit", "-m", title }
            : new[] { "commit", "-m", title, "-m", body };
        await repository.GitCliService.ExecuteBufferedAsync(arguments, repository.Path);
    }

    private static async Task<(string Head, byte[] Index, string Status)> CaptureAsync(
        TemporaryGitRepository repository) =>
        (
            await HeadAsync(repository),
            await File.ReadAllBytesAsync(Path.Combine(repository.Path, ".git", "index")),
            await GitOutputAsync(repository, "status", "--porcelain=v1")
        );

    private static async Task AssertUnchangedAsync(
        TemporaryGitRepository repository,
        (string Head, byte[] Index, string Status) state)
    {
        Assert.Equal(state.Head, await HeadAsync(repository));
        Assert.Equal(state.Index, await File.ReadAllBytesAsync(Path.Combine(repository.Path, ".git", "index")));
        Assert.Equal(state.Status, await GitOutputAsync(repository, "status", "--porcelain=v1"));
    }

    private static Task GitAsync(TemporaryGitRepository repository, params string[] arguments) =>
        repository.GitCliService.ExecuteBufferedAsync(arguments, repository.Path);

    private static async Task<string> GitOutputAsync(
        TemporaryGitRepository repository,
        params string[] arguments) =>
        (await repository.GitCliService.ExecuteBufferedAsync(arguments, repository.Path)).StandardOutput.Trim();

    private static Task<string> HeadAsync(TemporaryGitRepository repository) =>
        GitOutputAsync(repository, "rev-parse", "HEAD");

    private static string[] Lines(string value) =>
        value.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
}
