using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser;

namespace LovelyGit.Tests.Git.LovelyFastGitParser;

public sealed class GitRepositoryDiscoveryObjectFormatTests
{
    [Fact]
    public async Task ReadObjectFormatAsync_DefaultsToSha1WithoutAnExtensionsValue()
    {
        using var directory = new ConfigDirectory("[core]\n\tbare = false\n");

        var format = await ReadAsync(directory.Path, CancellationToken.None);

        Assert.Equal(GitObjectFormat.Sha1, format);
    }

    [Fact]
    public async Task ReadObjectFormatAsync_ReadsSha256FromTheSmallConfigFastPath()
    {
        using var directory = new ConfigDirectory(
            "[extensions]\n\tobjectFormat = sha256\n");

        var format = await ReadAsync(directory.Path, CancellationToken.None);

        Assert.Equal(GitObjectFormat.Sha256, format);
    }

    [Fact]
    public async Task ReadObjectFormatAsync_StreamsSha256AfterLongCrLfContentAndBom()
    {
        var longValue = new string('x', 20_000);
        using var directory = new ConfigDirectory(
            $"\uFEFF[core]\r\n\tcomment = {longValue}\r\n[extensions]\r\n\tobjectFormat = \"sha256\"\r\n");

        var format = await ReadAsync(directory.Path, CancellationToken.None);

        Assert.Equal(GitObjectFormat.Sha256, format);
    }

    [Fact]
    public async Task ReadObjectFormatAsync_RejectsAnUnsupportedFormat()
    {
        using var directory = new ConfigDirectory(
            "[extensions]\n\tobjectFormat = future-hash\n");

        var error = await Assert.ThrowsAsync<NotSupportedException>(() =>
            ReadAsync(directory.Path, CancellationToken.None));

        Assert.Contains("future-hash", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ReadObjectFormatAsync_HonorsCancellationWithoutMutation()
    {
        const string config = "[extensions]\n\tobjectFormat = sha256\n";
        using var directory = new ConfigDirectory(config);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            ReadAsync(directory.Path, cancellation.Token));

        Assert.Equal(config, await File.ReadAllTextAsync(
            System.IO.Path.Combine(directory.Path, "config")));
    }

    private static Task<GitObjectFormat> ReadAsync(string path, CancellationToken cancellationToken) =>
        GitRepositoryDiscovery.ReadObjectFormatAsync(path, cancellationToken);

    private sealed class ConfigDirectory : IDisposable
    {
        public ConfigDirectory(string config)
        {
            Path = Directory.CreateTempSubdirectory("lovelygit-object-format-").FullName;
            File.WriteAllText(System.IO.Path.Combine(Path, "config"), config);
        }

        public string Path { get; }

        public void Dispose() => Directory.Delete(Path, recursive: true);
    }
}
