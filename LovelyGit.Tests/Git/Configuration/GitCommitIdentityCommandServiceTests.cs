using ExpressThat.LovelyGit.Services.Git.Cli;
using ExpressThat.LovelyGit.Services.Git.Configuration;

namespace LovelyGit.Tests.Git.Configuration;

public sealed class GitCommitIdentityCommandServiceTests
{
    [Fact]
    public async Task SaveAndClearAsync_ManageOnlyRepositoryIdentity()
    {
        using var repository = TemporaryCommandRepository.Create();
        var service = CreateService();

        var saved = await service.SaveAsync(
            repository.Path,
            "LovelyGit User",
            "user@example.test",
            CancellationToken.None);

        Assert.Equal("LovelyGit User", saved.Name);
        Assert.Equal("user@example.test", saved.Email);
        Assert.Equal(GitIdentityValueSource.Repository, saved.NameSource);
        Assert.True(saved.HasRepositoryOverride);

        var cleared = await service.ClearAsync(repository.Path, CancellationToken.None);

        Assert.False(cleared.HasRepositoryOverride);
        var localConfig = await File.ReadAllTextAsync(
            System.IO.Path.Combine(repository.Path, ".git", "config"));
        Assert.DoesNotContain("LovelyGit User", localConfig, StringComparison.Ordinal);
        Assert.DoesNotContain("user@example.test", localConfig, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("", "user@example.test", "Enter the name")]
    [InlineData("User", "not-an-email", "valid commit email")]
    [InlineData("Line\nBreak", "user@example.test", "line break")]
    public async Task SaveAsync_RejectsInvalidIdentity(
        string name,
        string email,
        string expectedMessage)
    {
        using var repository = TemporaryCommandRepository.Create();

        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            CreateService().SaveAsync(repository.Path, name, email, CancellationToken.None));

        Assert.Contains(expectedMessage, exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    private static GitCommitIdentityCommandService CreateService() => new(
        new GitOperationService(new GitCliService()),
        new NativeGitCommitIdentityReader());
}

internal sealed class TemporaryCommandRepository : IDisposable
{
    private readonly DirectoryInfo _directory;

    private TemporaryCommandRepository(DirectoryInfo directory)
    {
        _directory = directory;
        Path = directory.FullName;
    }

    public string Path { get; }

    public static TemporaryCommandRepository Create()
    {
        var directory = Directory.CreateTempSubdirectory("lovelygit-identity-command-");
        var git = new GitCliService();
        git.ExecuteBufferedAsync(["init"], directory.FullName).GetAwaiter().GetResult();
        return new TemporaryCommandRepository(directory);
    }

    public void Dispose()
    {
        foreach (var file in _directory.EnumerateFiles("*", SearchOption.AllDirectories))
        {
            file.Attributes = FileAttributes.Normal;
        }

        _directory.Delete(recursive: true);
    }
}
