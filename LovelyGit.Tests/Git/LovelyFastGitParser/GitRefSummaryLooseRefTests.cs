using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser;
using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser.Refs;

namespace LovelyGit.Tests.Git.LovelyFastGitParser;

public sealed class GitRefSummaryLooseRefTests
{
    private const string ObjectId = "0123456789012345678901234567890123456789";

    [Fact]
    public async Task ReadAsync_ParsesTrimmedUppercaseLooseRefWithoutTemporaryText()
    {
        using var directory = new TemporaryGitDirectory();
        directory.WriteRef("heads/feature", $" \t{ObjectId.ToUpperInvariant()}\r\n");
        directory.WriteRef("heads/malformed", "not-an-object-id\n");

        var summary = await directory.ReadAsync(maxTags: 10);

        var reference = Assert.Single(summary.Refs);
        Assert.Equal("feature", reference.Name);
        Assert.Equal(ObjectId, reference.Target.ToString());
    }

    [Fact]
    public async Task LoadRefsAsync_UsesTheSharedLooseRefParser()
    {
        using var directory = new TemporaryGitDirectory();
        directory.WriteRef("heads/feature", $" \t{ObjectId.ToUpperInvariant()}\r\n");

        var refs = await GitRefReader.LoadRefsAsync(
            directory.Path,
            GitObjectFormat.Sha1,
            GitRefReader.DefaultTagLimit,
            CancellationToken.None);

        Assert.Equal(ObjectId, refs["refs/heads/feature"].Target.ToString());
    }

    [Fact]
    public async Task ReadAsync_PreservesLongWhitespaceFallback()
    {
        using var directory = new TemporaryGitDirectory();
        directory.WriteRef("heads/long", $"{new string(' ', 140)}{ObjectId}\n");

        var reference = Assert.Single((await directory.ReadAsync(maxTags: 10)).Refs);

        Assert.Equal(ObjectId, reference.Target.ToString());
    }

    [Fact]
    public async Task ReadAsync_AppliesTagLimitAcrossLooseAndPackedRefs()
    {
        using var directory = new TemporaryGitDirectory();
        directory.WriteRef("tags/loose", $"{ObjectId}\n");
        await File.WriteAllTextAsync(
            Path.Combine(directory.Path, "packed-refs"),
            $"{ObjectId} refs/tags/packed\n");

        var summary = await directory.ReadAsync(maxTags: 1);

        Assert.Single(summary.Refs, reference => reference.Kind == GitRefKind.Tag);
    }

    [Fact]
    public async Task ReadAsync_StopsAfterValidLooseTagLimitButKeepsAllBranches()
    {
        using var directory = new TemporaryGitDirectory();
        directory.WriteRef("tags/000-malformed", "not-an-object-id\n");
        directory.WriteRef("tags/001-first", $"{ObjectId}\n");
        directory.WriteRef("tags/002-beyond-limit", $"{ObjectId}\n");
        directory.WriteRef("heads/feature", $"{ObjectId}\n");

        var summary = await directory.ReadAsync(maxTags: 1);

        Assert.Contains(summary.Refs, reference => reference.Name == "feature");
        var tag = Assert.Single(summary.Refs, reference => reference.Kind == GitRefKind.Tag);
        Assert.Equal("001-first", tag.Name);
    }

    [Fact]
    public async Task ReadAsync_ReturnsEveryValidBranchAcrossConcurrentReads()
    {
        using var directory = new TemporaryGitDirectory();
        for (var index = 0; index < 64; index++)
        {
            directory.WriteRef($"heads/group/branch-{index:D2}", $"{ObjectId}\n");
        }
        directory.WriteRef("heads/group/malformed", "not-an-object-id\n");

        var summary = await directory.ReadAsync(maxTags: 10);

        Assert.Equal(64, summary.Refs.Count);
        Assert.Contains(summary.Refs, reference => reference.Name == "group/branch-63");
        Assert.DoesNotContain(summary.Refs, reference => reference.Name.EndsWith("malformed"));
    }

    [Fact]
    public async Task LoadRefsAsync_ExcludesLooseTagsBeyondRequestedLimit()
    {
        using var directory = new TemporaryGitDirectory();
        directory.WriteRef("tags/first", $"{ObjectId}\n");
        directory.WriteRef("tags/second", $"{ObjectId}\n");

        var refs = await GitRefReader.LoadRefsAsync(
            directory.Path,
            GitObjectFormat.Sha1,
            1,
            CancellationToken.None);

        Assert.Single(refs, reference => reference.Key.StartsWith("refs/tags/"));
    }

    private sealed class TemporaryGitDirectory : IDisposable
    {
        private readonly DirectoryInfo _directory =
            Directory.CreateTempSubdirectory("lovelygit-loose-refs-");

        public string Path => _directory.FullName;

        public void WriteRef(string relativePath, string contents)
        {
            var path = System.IO.Path.Combine(Path, "refs", relativePath);
            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path)!);
            File.WriteAllText(path, contents);
        }

        public Task<GitRefSummary> ReadAsync(int maxTags) =>
            GitRefSummaryReader.ReadAsync(
                Path,
                GitObjectFormat.Sha1,
                maxTags,
                CancellationToken.None);

        public void Dispose() => _directory.Delete(recursive: true);
    }
}
