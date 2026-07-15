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
