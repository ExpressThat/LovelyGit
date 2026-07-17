using ExpressThat.LovelyGit.Services.Git.WorkingTree;

namespace LovelyGit.Tests.Git.WorkingTree;

public sealed class GitIgnoreMatcherTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), $"lovelygit-ignore-{Guid.NewGuid():N}");
    private readonly string _gitDirectory;

    public GitIgnoreMatcherTests()
    {
        _gitDirectory = Path.Combine(_root, ".git");
        Directory.CreateDirectory(Path.Combine(_gitDirectory, "info"));
        var emptyGlobalExcludes = Path.Combine(_root, "global-excludes");
        File.WriteAllText(emptyGlobalExcludes, string.Empty);
        File.WriteAllText(
            Path.Combine(_gitDirectory, "config"),
            $"[core]{Environment.NewLine}\texcludesfile = {emptyGlobalExcludes}");
    }

    [Fact]
    public async Task RootRules_HandleCommonGitIgnoreGlobForms()
    {
        await File.WriteAllLinesAsync(
            Path.Combine(_root, ".gitignore"),
            ["*.log", "/root.txt", "build/", "temp?.txt", "[ab].cache", "docs/**/draft.md"]);
        var matcher = await LoadAsync();

        Assert.True(matcher.IsIgnored("nested/error.log", isDirectory: false));
        Assert.True(matcher.IsIgnored("root.txt", isDirectory: false));
        Assert.False(matcher.IsIgnored("nested/root.txt", isDirectory: false));
        Assert.True(matcher.IsIgnored("build/output.dll", isDirectory: false));
        Assert.True(matcher.IsIgnored("temp1.txt", isDirectory: false));
        Assert.False(matcher.IsIgnored("temp12.txt", isDirectory: false));
        Assert.True(matcher.IsIgnored("a.cache", isDirectory: false));
        Assert.True(matcher.IsIgnored("docs/guides/draft.md", isDirectory: false));
        Assert.True(matcher.IsIgnored("docs/draft.md", isDirectory: false));
    }

    [Fact]
    public async Task LaterNegation_ReincludesFilesOutsideIgnoredDirectories()
    {
        await File.WriteAllLinesAsync(
            Path.Combine(_root, ".gitignore"),
            ["*.log", "!keep.log", "build/", "!build/keep.log"]);
        var matcher = await LoadAsync();

        Assert.False(matcher.IsIgnored("keep.log", isDirectory: false));
        Assert.True(matcher.IsIgnored("other.log", isDirectory: false));
        Assert.True(matcher.IsIgnored("build/keep.log", isDirectory: false));
    }

    [Fact]
    public async Task EscapedMarkersAndSpaces_AreTreatedAsLiteralPatterns()
    {
        await File.WriteAllLinesAsync(
            Path.Combine(_root, ".gitignore"),
            [@"\#notes", @"\!important", @"file\ ", "ignored.txt   ", "# comment"]);
        var matcher = await LoadAsync();

        Assert.True(matcher.IsIgnored("#notes", isDirectory: false));
        Assert.True(matcher.IsIgnored("!important", isDirectory: false));
        Assert.True(matcher.IsIgnored("file ", isDirectory: false));
        Assert.True(matcher.IsIgnored("ignored.txt", isDirectory: false));
        Assert.False(matcher.IsIgnored("comment", isDirectory: false));
    }

    [Fact]
    public async Task EscapedGlobCharacters_AreMatchedLiterally()
    {
        await File.WriteAllTextAsync(
            Path.Combine(_root, ".gitignore"),
            @"/build/\[draft\]\ file\?.txt" + "\n");
        var matcher = await LoadAsync();

        Assert.True(matcher.IsIgnored("build/[draft] file?.txt", isDirectory: false));
        Assert.False(matcher.IsIgnored("build/d file1.txt", isDirectory: false));
    }

    [Fact]
    public async Task NestedRules_AreScopedToTheirDirectory()
    {
        Directory.CreateDirectory(Path.Combine(_root, "src"));
        await File.WriteAllTextAsync(Path.Combine(_root, "src", ".gitignore"), "generated/\n");
        var matcher = await LoadAsync();

        Assert.False(matcher.IsIgnored("src/generated/file.cs", isDirectory: false));
        await matcher.LoadRulesForDirectoryAsync(_root, "src", CancellationToken.None);
        Assert.True(matcher.IsIgnored("src/generated/file.cs", isDirectory: false));
        Assert.False(matcher.IsIgnored("generated/file.cs", isDirectory: false));
    }

    [Fact]
    public async Task SimpleSuffixRules_PreserveAnchoringDirectoryAndNestedScope()
    {
        await File.WriteAllLinesAsync(
            Path.Combine(_root, ".gitignore"),
            ["/*.root", "*.cache/"]);
        Directory.CreateDirectory(Path.Combine(_root, "src"));
        await File.WriteAllTextAsync(Path.Combine(_root, "src", ".gitignore"), "*.tmp\n");
        var matcher = await LoadAsync();
        await matcher.LoadRulesForDirectoryAsync(_root, "src", CancellationToken.None);

        Assert.True(matcher.IsIgnored("file.root", isDirectory: false));
        Assert.False(matcher.IsIgnored("nested/file.root", isDirectory: false));
        Assert.True(matcher.IsIgnored("build.cache/output.bin", isDirectory: false));
        Assert.True(matcher.IsIgnored("src/nested/file.tmp", isDirectory: false));
        Assert.False(matcher.IsIgnored("nested/file.tmp", isDirectory: false));
    }

    [Fact]
    public async Task RepositoryAndConfiguredExcludes_AreLoaded()
    {
        var configuredExcludes = Path.Combine(_root, "configured-excludes");
        await File.WriteAllTextAsync(configuredExcludes, "*.secret\n");
        await File.WriteAllTextAsync(
            Path.Combine(_gitDirectory, "config"),
            $"[core]{Environment.NewLine}\texcludesfile = \"{configuredExcludes}\"");
        await File.WriteAllTextAsync(Path.Combine(_gitDirectory, "info", "exclude"), "*.local\n");

        var matcher = await LoadAsync();

        Assert.True(matcher.IsIgnored("token.secret", isDirectory: false));
        Assert.True(matcher.IsIgnored("settings.local", isDirectory: false));
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }

    private Task<GitIgnoreMatcher> LoadAsync() =>
        GitIgnoreMatcher.LoadAsync(_root, _gitDirectory, CancellationToken.None);
}
