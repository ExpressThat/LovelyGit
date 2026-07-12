using ExpressThat.LovelyGit.Services.Git.WorkingTree;

namespace LovelyGit.Tests.Git.WorkingTree;

public sealed class GitIgnoreServiceTests
{
    [Fact]
    public async Task AddExactPathAsync_AddsEscapedSharedRuleThatGitHonors()
    {
        using var repository = await CreateRepositoryAsync();
        var relativePath = "build/[draft] file.txt";
        Directory.CreateDirectory(Path.Combine(repository.Path, "build"));
        await File.WriteAllTextAsync(Path.Combine(repository.Path, relativePath), "draft");
        var nativeChanges = new WorkingTreeChangeService();
        Assert.Contains(
            (await nativeChanges.GetChangesAsync(repository.Path, CancellationToken.None)).Untracked,
            file => file.Path == relativePath);

        var result = await new GitIgnoreService().AddExactPathAsync(
            repository.Path,
            relativePath,
            GitIgnoreTarget.Shared,
            CancellationToken.None);

        Assert.True(result.Added);
        Assert.Equal(@"/build/\[draft\]\ file.txt", result.Pattern);
        Assert.Contains(result.Pattern, await File.ReadAllLinesAsync(
            Path.Combine(repository.Path, ".gitignore")));
        var ignored = await GitTestProcess.RunAsync(
            repository.Path,
            "check-ignore",
            "--",
            relativePath);
        Assert.Equal(relativePath, ignored.Trim());
        Assert.DoesNotContain(
            (await nativeChanges.GetChangesAsync(repository.Path, CancellationToken.None)).Untracked,
            file => file.Path == relativePath);
    }

    [Fact]
    public async Task AddExactPathAsync_AddsLocalRuleOnce()
    {
        using var repository = await CreateRepositoryAsync();
        var service = new GitIgnoreService();

        var added = await service.AddExactPathAsync(
            repository.Path,
            "notes.local",
            GitIgnoreTarget.Local,
            CancellationToken.None);
        var duplicate = await service.AddExactPathAsync(
            repository.Path,
            "notes.local",
            GitIgnoreTarget.Local,
            CancellationToken.None);

        Assert.True(added.Added);
        Assert.False(duplicate.Added);
        var lines = await File.ReadAllLinesAsync(
            Path.Combine(repository.Path, ".git", "info", "exclude"));
        Assert.Equal(1, lines.Count(line => line == "/notes.local"));
    }

    [Theory]
    [InlineData("../outside.txt")]
    [InlineData(".git/config")]
    [InlineData("unsafe\nrule.txt")]
    public async Task AddExactPathAsync_RejectsUnsafePaths(string path)
    {
        using var directory = TemporaryDirectory.Create("lovelygit-invalid-ignore-");
        var sentinel = Path.Combine(directory.Path, "sentinel.txt");
        await File.WriteAllTextAsync(sentinel, "unchanged");

        await Assert.ThrowsAsync<ArgumentException>(() => new GitIgnoreService().AddExactPathAsync(
            directory.Path,
            path,
            GitIgnoreTarget.Shared,
            CancellationToken.None));
        Assert.Equal("unchanged", await File.ReadAllTextAsync(sentinel));
        Assert.False(File.Exists(Path.Combine(directory.Path, ".gitignore")));
    }

    [Fact]
    public void BuildExactPattern_EscapesLiteralBackslashes()
    {
        Assert.Equal(@"/folder\\name.txt", GitIgnoreService.BuildExactPattern(@"folder\name.txt"));
    }

    private static async Task<TemporaryDirectory> CreateRepositoryAsync()
    {
        var repository = TemporaryDirectory.Create("lovelygit-ignore-command-");
        await GitTestProcess.RunAsync(repository.Path, "init");
        return repository;
    }
}
