using ExpressThat.LovelyGit.Services.Git.Cli;
using ExpressThat.LovelyGit.Services.Git.SparseCheckout;

namespace LovelyGit.Tests.Git.SparseCheckout;

public sealed class SparseCheckoutServicesTests
{
    [Fact]
    public async Task Reader_ReturnsDisabledStateWithoutInvokingGit()
    {
        using var repository = SparseRepository.Create();

        var state = await new NativeSparseCheckoutReader().ReadAsync(
            repository.Path,
            CancellationToken.None);

        Assert.False(state.Enabled);
        Assert.Equal(0, state.PatternCount);
        Assert.Empty(state.PatternText);
    }

    [Fact]
    public async Task SetConeMode_ReturnsOnlyExplicitSelectionsAndShrinksWorktree()
    {
        using var repository = SparseRepository.Create();
        var service = CreateService();

        var state = await service.ExecuteAsync(
            repository.Path,
            SparseCheckoutAction.Set,
            coneMode: true,
            "src/feature\ndocs",
            CancellationToken.None);

        Assert.True(state.Enabled);
        Assert.True(state.ConeMode);
        Assert.Equal(2, state.PatternCount);
        Assert.Equal(["docs", "src/feature"], state.PatternText.Split('\n').Order());
        Assert.True(File.Exists(Path.Combine(repository.Path, "src", "feature", "file.txt")));
        Assert.False(File.Exists(Path.Combine(repository.Path, "src", "other", "file.txt")));
    }

    [Fact]
    public async Task SetPatternMode_PreservesGitIgnoreStylePatterns()
    {
        using var repository = SparseRepository.Create();

        var state = await CreateService().ExecuteAsync(
            repository.Path,
            SparseCheckoutAction.Set,
            coneMode: false,
            "/*\n!/src/other/",
            CancellationToken.None);

        Assert.True(state.Enabled);
        Assert.False(state.ConeMode);
        Assert.Equal(2, state.PatternCount);
        Assert.Equal("/*\n!/src/other/", state.PatternText);
    }

    [Fact]
    public async Task Disable_RestoresTheFullWorktree()
    {
        using var repository = SparseRepository.Create();
        var service = CreateService();
        await service.ExecuteAsync(
            repository.Path,
            SparseCheckoutAction.Set,
            true,
            "docs",
            CancellationToken.None);

        var state = await service.ExecuteAsync(
            repository.Path,
            SparseCheckoutAction.Disable,
            false,
            null,
            CancellationToken.None);

        Assert.False(state.Enabled);
        Assert.True(File.Exists(Path.Combine(repository.Path, "src", "other", "file.txt")));
    }

    [Theory]
    [InlineData("../outside")]
    [InlineData(".git")]
    [InlineData("/absolute")]
    public async Task InvalidConePath_DoesNotMutateRepository(string path)
    {
        var directory = Directory.CreateTempSubdirectory("lovelygit-invalid-sparse-");
        var sentinel = Path.Combine(directory.FullName, "sentinel.txt");
        await File.WriteAllTextAsync(sentinel, "unchanged");

        try
        {
            await Assert.ThrowsAsync<ArgumentException>(() => CreateService().ExecuteAsync(
                directory.FullName,
                SparseCheckoutAction.Set,
                true,
                path,
                CancellationToken.None));

            Assert.Equal("unchanged", await File.ReadAllTextAsync(sentinel));
            Assert.False(Directory.Exists(Path.Combine(directory.FullName, ".git")));
        }
        finally
        {
            directory.Delete(recursive: true);
        }
    }

    [Fact]
    public async Task GitFailure_PreservesFullCheckoutAndSparseState()
    {
        using var repository = SparseRepository.Create();
        var failingGit = new GitCliService(new Dictionary<string, string?>
        {
            ["GIT_INDEX_FILE"] = repository.Path,
        });

        await Assert.ThrowsAsync<InvalidOperationException>(() => CreateService(failingGit).ExecuteAsync(
            repository.Path,
            SparseCheckoutAction.Set,
            true,
            "docs",
            CancellationToken.None));

        var state = await new NativeSparseCheckoutReader().ReadAsync(
            repository.Path,
            CancellationToken.None);
        Assert.False(state.Enabled);
        Assert.True(File.Exists(Path.Combine(repository.Path, "src", "other", "file.txt")));
    }

    [Fact]
    public void BuildArguments_UsesStandardInputForPatterns()
    {
        var arguments = GitSparseCheckoutCommandService.BuildArguments(
            SparseCheckoutAction.Set,
            false);

        Assert.Equal("--stdin", arguments[^1]);
    }

    [Fact]
    public void ConeParser_RemovesGeneratedParentDirectories()
    {
        var patterns = NativeSparseCheckoutReader.ReadPatterns(
            ["/*", "!/*/", "/src/", "!/src/*/", "/docs/", "/src/feature/"],
            coneMode: true);

        Assert.Equal(["docs", "src/feature"], patterns);
    }

    [Fact]
    public void PatternParser_TrimsPatternsAndIgnoresComments()
    {
        var patterns = NativeSparseCheckoutReader.ReadPatterns(
            [" # comment", "  /src/**  ", "", "\t!/generated/**"],
            coneMode: false);

        Assert.Equal(["/src/**", "!/generated/**"], patterns);
    }

    [Fact]
    public async Task CancelledRead_DoesNotChangeSparseSpecification()
    {
        using var repository = SparseRepository.Create();
        await CreateService().ExecuteAsync(
            repository.Path,
            SparseCheckoutAction.Set,
            false,
            "/*\n!/src/other/",
            CancellationToken.None);
        var specification = Path.Combine(repository.Path, ".git", "info", "sparse-checkout");
        var before = await File.ReadAllBytesAsync(specification);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            new NativeSparseCheckoutReader().ReadAsync(repository.Path, cancellation.Token));

        Assert.Equal(before, await File.ReadAllBytesAsync(specification));
    }

    private static GitSparseCheckoutCommandService CreateService(GitCliService? git = null)
    {
        var reader = new NativeSparseCheckoutReader();
        return new GitSparseCheckoutCommandService(git ?? new GitCliService(), reader);
    }
}

internal sealed class SparseRepository : IDisposable
{
    private readonly DirectoryInfo _directory;
    private readonly GitCliService _git = new();

    private SparseRepository(DirectoryInfo directory)
    {
        _directory = directory;
        Path = directory.FullName;
    }

    public string Path { get; }

    public static SparseRepository Create()
    {
        var repository = new SparseRepository(
            Directory.CreateTempSubdirectory("lovelygit-sparse-"));
        repository.Run(["init", "-b", "main"]);
        repository.Run(["config", "user.name", "LovelyGit Test"]);
        repository.Run(["config", "user.email", "test@example.invalid"]);
        repository.Write("src/feature/file.txt");
        repository.Write("src/other/file.txt");
        repository.Write("docs/readme.md");
        repository.Run(["add", "."]);
        repository.Run(["commit", "-m", "Initial"]);
        return repository;
    }

    public void Dispose()
    {
        foreach (var file in _directory.EnumerateFiles("*", SearchOption.AllDirectories))
        {
            file.Attributes = FileAttributes.Normal;
        }
        _directory.Delete(recursive: true);
    }

    private void Write(string relativePath)
    {
        var path = System.IO.Path.Combine(Path, relativePath.Replace('/', System.IO.Path.DirectorySeparatorChar));
        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path)!);
        File.WriteAllText(path, relativePath);
    }

    private void Run(IReadOnlyList<string> arguments) =>
        _git.ExecuteBufferedAsync(arguments, Path).GetAwaiter().GetResult();

}
