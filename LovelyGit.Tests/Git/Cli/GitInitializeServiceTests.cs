using ExpressThat.LovelyGit.Services.Git.Cli;

namespace LovelyGit.Tests.Git.Cli;

public sealed class GitInitializeServiceTests
{
    [Fact]
    public async Task InitializeAsync_CreatesRepositoryWithRequestedInitialBranch()
    {
        using var parent = TemporaryDirectory.Create("lovelygit-init-success-");
        var service = new GitInitializeService(new GitCliService());

        var path = await service.InitializeAsync(
            parent.Path,
            "new-repository",
            "trunk",
            CancellationToken.None,
            CommitIdentity());

        Assert.True(Directory.Exists(Path.Combine(path, ".git")));
        var branch = await new GitCliService().ExecuteBufferedAsync(
            ["symbolic-ref", "--short", "HEAD"], path);
        Assert.Equal("trunk", branch.StandardOutput.Trim());
        var subject = await new GitCliService().ExecuteBufferedAsync(
            ["log", "-1", "--format=%s"], path);
        Assert.Equal("Initial commit", subject.StandardOutput.Trim());
    }

    [Theory]
    [InlineData("", "repo", "main", "Destination folder is required")]
    [InlineData("valid", "..", "main", "folder name is not valid")]
    [InlineData("valid", "nested/repo", "main", "folder name is not valid")]
    [InlineData("valid", "repo", "bad branch", "branch name is not valid")]
    [InlineData("valid", "repo", "-bad", "branch name is not valid")]
    [InlineData("valid", "repo", ".hidden", "branch name is not valid")]
    [InlineData("valid", "repo", "feature..bad", "branch name is not valid")]
    [InlineData("valid", "repo", "feature/@{1", "branch name is not valid")]
    [InlineData("valid", "repo", "main.lock", "branch name is not valid")]
    public async Task InitializeAsync_RejectsInvalidInputWithoutCreatingRepository(
        string parentPath,
        string directoryName,
        string initialBranch,
        string expectedMessage)
    {
        using var parent = TemporaryDirectory.Create("lovelygit-init-validation-");
        var resolvedParent = parentPath == "valid" ? parent.Path : parentPath;
        var service = new GitInitializeService(new GitCliService());

        var exception = await Assert.ThrowsAnyAsync<ArgumentException>(() =>
            service.InitializeAsync(
                resolvedParent,
                directoryName,
                initialBranch,
                CancellationToken.None));

        Assert.Contains(expectedMessage, exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(Directory.EnumerateFileSystemEntries(parent.Path));
    }

    [Fact]
    public async Task InitializeAsync_RejectsMissingParentWithoutCreatingDestination()
    {
        using var root = TemporaryDirectory.Create("lovelygit-init-missing-");
        var missingParent = Path.Combine(root.Path, "missing");

        var exception = await Assert.ThrowsAsync<DirectoryNotFoundException>(() =>
            new GitInitializeService(new GitCliService()).InitializeAsync(
                missingParent,
                "repo",
                "main",
                CancellationToken.None));

        Assert.Contains("does not exist", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.False(Directory.Exists(missingParent));
    }

    [Fact]
    public async Task InitializeAsync_LeavesExistingDestinationUnchanged()
    {
        using var parent = TemporaryDirectory.Create("lovelygit-init-existing-");
        var destination = Directory.CreateDirectory(Path.Combine(parent.Path, "repo"));
        var sentinel = Path.Combine(destination.FullName, "keep.txt");
        await File.WriteAllTextAsync(sentinel, "unchanged");

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new GitInitializeService(new GitCliService()).InitializeAsync(
                parent.Path,
                "repo",
                "main",
                CancellationToken.None));

        Assert.Contains("already exists", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("unchanged", await File.ReadAllTextAsync(sentinel));
    }

    [Fact]
    public async Task InitializeAsync_CanceledValidationDoesNotCreateDestination()
    {
        using var parent = TemporaryDirectory.Create("lovelygit-init-cancel-");
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            new GitInitializeService(new GitCliService()).InitializeAsync(
                parent.Path,
                "repo",
                "main",
                cancellation.Token));

        Assert.Empty(Directory.EnumerateFileSystemEntries(parent.Path));
    }

    [Fact]
    public async Task InitializeAsync_MissingIdentityRemovesInitializedRepository()
    {
        using var parent = TemporaryDirectory.Create("lovelygit-init-identity-");
        var destination = Path.Combine(parent.Path, "repo");
        var environment = new Dictionary<string, string?>
        {
            ["GIT_CONFIG_NOSYSTEM"] = "1",
            ["HOME"] = parent.Path,
            ["USERPROFILE"] = parent.Path,
        };

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new GitInitializeService(new GitCliService()).InitializeAsync(
                parent.Path,
                "repo",
                "main",
                CancellationToken.None,
                environment));

        Assert.Contains("identity", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.False(Directory.Exists(destination));
    }

    private static IReadOnlyDictionary<string, string?> CommitIdentity() =>
        new Dictionary<string, string?>
        {
            ["GIT_AUTHOR_NAME"] = "LovelyGit Init Test",
            ["GIT_AUTHOR_EMAIL"] = "init@example.test",
            ["GIT_COMMITTER_NAME"] = "LovelyGit Init Test",
            ["GIT_COMMITTER_EMAIL"] = "init@example.test",
        };
}
