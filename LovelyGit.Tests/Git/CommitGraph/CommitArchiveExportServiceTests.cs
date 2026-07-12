using System.Formats.Tar;
using System.IO.Compression;
using ExpressThat.LovelyGit.Services.Dialogs;
using ExpressThat.LovelyGit.Services.Git.Cli;
using ExpressThat.LovelyGit.Services.Git.CommitGraph;
using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser;

namespace LovelyGit.Tests.Git.CommitGraph;

public sealed class CommitArchiveExportServiceTests
{
    [Fact]
    public async Task ExportAsync_WritesZipContainingTheSelectedCommitTree()
    {
        using var repository = TemporaryArchiveRepository.Create();
        repository.WriteFile("old.txt", "old");
        repository.WriteBytes("asset.bin", [0, 255, 16, 32]);
        var selectedHash = repository.Commit("Selected");
        repository.WriteFile("later.txt", "later");
        repository.Commit("Later");
        var output = Path.Combine(repository.Path, "selected.zip");

        var response = await CreateService(output).ExportAsync(
            repository.Path,
            GitObjectId.Parse(selectedHash),
            CancellationToken.None);

        Assert.True(response.Saved);
        using var archive = ZipFile.OpenRead(output);
        Assert.Contains(archive.Entries, entry => entry.FullName == "old.txt");
        var binaryEntry = Assert.Single(archive.Entries, entry => entry.FullName == "asset.bin");
        await using var binaryStream = binaryEntry.Open();
        using var binaryBuffer = new MemoryStream();
        await binaryStream.CopyToAsync(binaryBuffer);
        Assert.Equal([0, 255, 16, 32], binaryBuffer.ToArray());
        Assert.DoesNotContain(archive.Entries, entry => entry.FullName == "later.txt");
    }

    [Fact]
    public async Task ExportAsync_WritesTarWhenTarExtensionIsSelected()
    {
        using var repository = TemporaryArchiveRepository.Create();
        repository.WriteFile("note.txt", "archive me");
        var hash = repository.Commit("Archive");
        var output = Path.Combine(repository.Path, "selected.tar");

        await CreateService(output).ExportAsync(
            repository.Path,
            GitObjectId.Parse(hash),
            CancellationToken.None);

        using var stream = File.OpenRead(output);
        using var reader = new TarReader(stream);
        var names = new List<string>();
        while (reader.GetNextEntry() is { } entry)
        {
            names.Add(entry.Name);
        }
        Assert.Contains("note.txt", names);
    }

    [Fact]
    public async Task ExportAsync_WhenPickerIsCancelled_DoesNotCreateAnArchive()
    {
        using var repository = TemporaryArchiveRepository.Create();
        repository.WriteFile("note.txt", "content");
        var hash = repository.Commit("Initial");
        var picker = new StubSaveFilePicker(null);

        var response = await new CommitArchiveExportService(new GitCliService(), picker)
            .ExportAsync(repository.Path, GitObjectId.Parse(hash), CancellationToken.None);

        Assert.False(response.Saved);
        Assert.Null(response.Path);
        Assert.Equal(["zip", "tar"], picker.Extensions);
    }

    [Fact]
    public async Task ExportAsync_WhenGitFails_PreservesExistingDestinationAndRemovesTemporaryFile()
    {
        using var repository = TemporaryArchiveRepository.Create();
        var output = Path.Combine(repository.Path, "existing.zip");
        await File.WriteAllTextAsync(output, "keep me");

        await Assert.ThrowsAnyAsync<Exception>(() => CreateService(output).ExportAsync(
            repository.Path,
            GitObjectId.Parse("1".PadLeft(40, '1')),
            CancellationToken.None));

        Assert.Equal("keep me", await File.ReadAllTextAsync(output));
        Assert.Empty(Directory.EnumerateFiles(repository.Path, ".lovelygit-*.tmp"));
    }

    [Fact]
    public async Task ExportAsync_WhenCancelled_PreservesExistingDestination()
    {
        using var repository = TemporaryArchiveRepository.Create();
        repository.WriteFile("note.txt", "content");
        var hash = repository.Commit("Initial");
        var output = Path.Combine(repository.Path, "existing.zip");
        await File.WriteAllTextAsync(output, "keep me");
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => CreateService(output)
            .ExportAsync(repository.Path, GitObjectId.Parse(hash), cancellation.Token));

        Assert.Equal("keep me", await File.ReadAllTextAsync(output));
        Assert.Empty(Directory.EnumerateFiles(repository.Path, ".lovelygit-*.tmp"));
    }

    private static CommitArchiveExportService CreateService(string? output) =>
        new(new GitCliService(), new StubSaveFilePicker(output));

    private sealed class StubSaveFilePicker(string? path) : ISaveFilePicker
    {
        public IReadOnlyList<string>? Extensions { get; private set; }

        public Task<string?> PickSaveFileAsync(
            string title,
            string suggestedFileName,
            IReadOnlyList<string> extensions,
            CancellationToken cancellationToken)
        {
            Assert.Equal("Export commit archive", title);
            Assert.EndsWith(".zip", suggestedFileName, StringComparison.Ordinal);
            Extensions = extensions;
            return Task.FromResult(path);
        }
    }
}

internal sealed class TemporaryArchiveRepository : IDisposable
{
    private static readonly RepositoryTemplate<bool> Template = new(
        "lovelygit-archive-export-template-",
        InitializeTemplate);
    private readonly DirectoryInfo _directory;
    private readonly GitCliService _git = new();

    private TemporaryArchiveRepository(DirectoryInfo directory)
    {
        _directory = directory;
        Path = directory.FullName;
    }

    public string Path { get; }

    public static TemporaryArchiveRepository Create()
    {
        var (directory, _) = Template.CreateCopy("lovelygit-archive-export-");
        return new TemporaryArchiveRepository(directory);
    }

    private static bool InitializeTemplate(DirectoryInfo directory)
    {
        var repository = new TemporaryArchiveRepository(directory);
        InitializedRepositoryTemplate.CopyInto(directory, "master");
        return true;
    }

    public void WriteFile(string relativePath, string contents) =>
        File.WriteAllText(System.IO.Path.Combine(Path, relativePath), contents);

    public void WriteBytes(string relativePath, byte[] contents) =>
        File.WriteAllBytes(System.IO.Path.Combine(Path, relativePath), contents);

    public string Commit(string message)
    {
        RunGit(["add", "."]);
        RunGit(["commit", "-m", message]);
        return RunGit(["rev-parse", "HEAD"]).StandardOutput.Trim();
    }

    public void Dispose()
    {
        foreach (var file in _directory.EnumerateFiles("*", SearchOption.AllDirectories))
        {
            file.Attributes = FileAttributes.Normal;
        }
        _directory.Delete(recursive: true);
    }

    private CliWrap.Buffered.BufferedCommandResult RunGit(IReadOnlyList<string> arguments) =>
        _git.ExecuteBufferedAsync(arguments, Path).GetAwaiter().GetResult();
}
