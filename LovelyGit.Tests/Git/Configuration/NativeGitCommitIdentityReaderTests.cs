using ExpressThat.LovelyGit.Services.Git.Configuration;

namespace LovelyGit.Tests.Git.Configuration;

public sealed class NativeGitCommitIdentityReaderTests
{
    [Fact]
    public void ResolveSystemPaths_UsesTheSelectedGitInstallation()
    {
        var root = Path.Combine("C:", "LovelyGit", "Git");

        var paths = GitIdentityReadOptions.ResolveSystemPaths(
            new Dictionary<string, string?>(), root);

        Assert.Equal([Path.Combine(root, "etc", "gitconfig")], paths);
    }

    [Fact]
    public async Task ReadAsync_ResolvesIncludesAndRepositoryPrecedence()
    {
        using var repository = TestIdentityRepository.Create();
        var profilePath = Path.Combine(repository.HomePath, "work-profile.inc");
        await File.WriteAllTextAsync(profilePath, "[user]\n\tname = Work Profile\n");
        await File.WriteAllTextAsync(
            repository.GlobalConfigPath,
            $"[user]\n\tname = Global User\n\temail = global@example.test\n" +
            $"[includeIf \"gitdir/i:{Normalize(repository.GitDirectory)}/\"]\n" +
            "\tpath = ./work-profile.inc\n");
        await repository.AppendLocalConfigAsync("[user]\n\temail = local@example.test\n");

        var identity = await new NativeGitCommitIdentityReader().ReadAsync(
            repository.Path,
            repository.CreateOptions(),
            CancellationToken.None);

        Assert.Equal("Work Profile", identity.Name);
        Assert.Equal(GitIdentityValueSource.Global, identity.NameSource);
        Assert.Equal("local@example.test", identity.Email);
        Assert.Equal(GitIdentityValueSource.Repository, identity.EmailSource);
        Assert.True(identity.HasRepositoryOverride);
        Assert.True(identity.IsComplete);
    }

    [Fact]
    public async Task ReadAsync_AppliesMatchingBranchProfile()
    {
        using var repository = TestIdentityRepository.Create();
        await File.WriteAllTextAsync(
            repository.GlobalConfigPath,
            "[includeIf \"onbranch:main\"]\n\tpath = ./main.inc\n");
        await File.WriteAllTextAsync(
            Path.Combine(repository.HomePath, "main.inc"),
            "[user]\n\tname = Main Branch\n\temail = main@example.test\n");

        var identity = await new NativeGitCommitIdentityReader().ReadAsync(
            repository.Path,
            repository.CreateOptions(),
            CancellationToken.None);

        Assert.Equal("Main Branch", identity.Name);
        Assert.Equal("main@example.test", identity.Email);
    }

    [Fact]
    public async Task ReadAsync_PreservesIdentityAndIncludeOrdering()
    {
        using var repository = TestIdentityRepository.Create();
        await File.WriteAllTextAsync(
            repository.GlobalConfigPath,
            "[include]\n\tpath = ./included.inc\n" +
            "[user]\n\tname = After Include\n");
        await File.WriteAllTextAsync(
            Path.Combine(repository.HomePath, "included.inc"),
            "[user]\n\tname = Included User\n\temail = included@example.test\n");

        var identity = await new NativeGitCommitIdentityReader().ReadAsync(
            repository.Path,
            repository.CreateOptions(),
            CancellationToken.None);

        Assert.Equal("After Include", identity.Name);
        Assert.Equal("included@example.test", identity.Email);
    }

    [Fact]
    public async Task ReadAsync_EnvironmentWinsWithoutHidingLocalOverride()
    {
        using var repository = TestIdentityRepository.Create();
        await repository.AppendLocalConfigAsync(
            "[user]\n\tname = Local User\n\temail = local@example.test\n");
        var environment = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
        {
            ["GIT_COMMITTER_NAME"] = "Build Agent",
            ["GIT_COMMITTER_EMAIL"] = "agent@example.test",
        };

        var identity = await new NativeGitCommitIdentityReader().ReadAsync(
            repository.Path,
            repository.CreateOptions(environment),
            CancellationToken.None);

        Assert.Equal("Build Agent", identity.Name);
        Assert.Equal("agent@example.test", identity.Email);
        Assert.Equal(GitIdentityValueSource.Environment, identity.NameSource);
        Assert.Equal(GitIdentityValueSource.Environment, identity.EmailSource);
        Assert.True(identity.HasRepositoryOverride);
    }

    [Fact]
    public async Task ReadAsync_AppliesCommandEnvironmentConfiguration()
    {
        using var repository = TestIdentityRepository.Create();
        var environment = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
        {
            ["GIT_CONFIG_COUNT"] = "2",
            ["GIT_CONFIG_KEY_0"] = "user.name",
            ["GIT_CONFIG_VALUE_0"] = "Configured Name",
            ["GIT_CONFIG_KEY_1"] = "user.email",
            ["GIT_CONFIG_VALUE_1"] = "configured@example.test",
        };

        var identity = await new NativeGitCommitIdentityReader().ReadAsync(
            repository.Path,
            repository.CreateOptions(environment),
            CancellationToken.None);

        Assert.Equal("Configured Name", identity.Name);
        Assert.Equal("configured@example.test", identity.Email);
        Assert.Equal(GitIdentityValueSource.Environment, identity.NameSource);
    }

    [Fact]
    public async Task ReadAsync_UsesWorktreeConfigOnlyWhenEnabled()
    {
        using var repository = TestIdentityRepository.Create();
        var worktreeConfig = Path.Combine(repository.GitDirectory, "config.worktree");
        await File.WriteAllTextAsync(
            worktreeConfig,
            "[user]\n\tname = Worktree User\n\temail = worktree@example.test\n");
        var reader = new NativeGitCommitIdentityReader();

        var disabled = await reader.ReadAsync(
            repository.Path, repository.CreateOptions(), CancellationToken.None);
        await repository.AppendLocalConfigAsync(
            "[extensions]\n\tworktreeConfig = true\n");
        var enabled = await reader.ReadAsync(
            repository.Path, repository.CreateOptions(), CancellationToken.None);

        Assert.False(disabled.IsComplete);
        Assert.Equal("Worktree User", enabled.Name);
        Assert.Equal(GitIdentityValueSource.Worktree, enabled.NameSource);
        Assert.True(enabled.HasRepositoryOverride);
    }

    [Fact]
    public async Task ReadAsync_StopsRecursiveIncludesWithoutLosingLaterValues()
    {
        using var repository = TestIdentityRepository.Create();
        await File.WriteAllTextAsync(
            repository.GlobalConfigPath,
            "[include]\n\tpath = ./.gitconfig\n" +
            "[user]\n\tname = Cycle Safe\n\temail = cycle@example.test\n");

        var identity = await new NativeGitCommitIdentityReader().ReadAsync(
            repository.Path, repository.CreateOptions(), CancellationToken.None);

        Assert.Equal("Cycle Safe", identity.Name);
        Assert.Equal("cycle@example.test", identity.Email);
    }

    [Fact]
    public async Task ConfigParser_HonorsPreCanceledReads()
    {
        using var repository = TestIdentityRepository.Create();
        await File.WriteAllTextAsync(repository.GlobalConfigPath, "[user]\nname = Canceled\n");
        var parser = new GitIdentityConfigParser(
            repository.GitDirectory, "main", repository.HomePath);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => parser.ReadAsync(
            repository.GlobalConfigPath,
            GitIdentityValueSource.Global,
            new GitIdentityAccumulator(),
            new CancellationToken(canceled: true)));
    }

    private static string Normalize(string path) => path.Replace('\\', '/');
}

internal sealed class TestIdentityRepository : IDisposable
{
    private readonly DirectoryInfo _root;

    private TestIdentityRepository(DirectoryInfo root)
    {
        _root = root;
        Path = System.IO.Path.Combine(root.FullName, "repository");
        HomePath = System.IO.Path.Combine(root.FullName, "home");
        GitDirectory = System.IO.Path.Combine(Path, ".git");
        GlobalConfigPath = System.IO.Path.Combine(HomePath, ".gitconfig");
    }

    public string Path { get; }
    public string HomePath { get; }
    public string GitDirectory { get; }
    public string GlobalConfigPath { get; }

    public Task AppendLocalConfigAsync(string text) =>
        File.AppendAllTextAsync(System.IO.Path.Combine(GitDirectory, "config"), text);

    public GitIdentityReadOptions CreateOptions(
        IReadOnlyDictionary<string, string?>? environment = null) => new(
        HomePath,
        null,
        [],
        [GlobalConfigPath],
        environment ?? new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase));

    public static TestIdentityRepository Create()
    {
        var root = Directory.CreateTempSubdirectory("lovelygit-identity-reader-");
        var repository = new TestIdentityRepository(root);
        Directory.CreateDirectory(repository.GitDirectory);
        Directory.CreateDirectory(repository.HomePath);
        File.WriteAllText(
            System.IO.Path.Combine(repository.GitDirectory, "config"),
            "[core]\n\trepositoryformatversion = 0\n");
        File.WriteAllText(
            System.IO.Path.Combine(repository.GitDirectory, "HEAD"),
            "ref: refs/heads/main\n");
        return repository;
    }

    public void Dispose()
    {
        foreach (var file in _root.EnumerateFiles("*", SearchOption.AllDirectories))
        {
            file.Attributes = FileAttributes.Normal;
        }

        _root.Delete(recursive: true);
    }
}
