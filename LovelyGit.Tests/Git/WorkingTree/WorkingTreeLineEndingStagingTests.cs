using System.Security.Cryptography;
using System.Text;
using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser;
using ExpressThat.LovelyGit.Services.Git.WorkingTree;
using ExpressThat.LovelyGit.Services.Git.WorkingTree.Models;
using LovelyGit.Tests.Git.RepositoryOperations;

namespace LovelyGit.Tests.Git.WorkingTree;

public sealed class WorkingTreeLineEndingStagingTests
{
    [Fact]
    public async Task StageAndUnstage_PreserveExactLineEndingsAcrossLineAndHunkActions()
    {
        using var repository = TestRepository.Create();
        await PrepareFilesAsync(repository);
        var service = new WorkingTreeIndexService(repository.Git);

        await VerifyLineAsync(
            repository, service, "crlf.txt", 1, "one", "ONE", "\r\n");
        await VerifyLineAsync(
            repository, service, "no-final-newline.txt", 2, "last", "LAST", "");
        await VerifyMixedHunkAsync(repository, service);
    }

    private static async Task VerifyLineAsync(
        TestRepository repository,
        WorkingTreeIndexService service,
        string path,
        int line,
        string oldText,
        string newText,
        string ending)
    {
        await service.StageLineAsync(
            repository.Path, path, "Unstaged", "Modified", line, line,
            oldText, newText, ending, ending, CancellationToken.None);
        await AssertIndexAsync(repository, path, ChangedContent(path));

        await service.UnstageLineAsync(
            repository.Path, path, "Modified", line, line,
            oldText, newText, ending, ending, CancellationToken.None);
        await AssertIndexAsync(repository, path, BaselineContent(path));
    }

    private static async Task VerifyMixedHunkAsync(
        TestRepository repository,
        WorkingTreeIndexService service)
    {
        const string path = "mixed.txt";
        WorkingTreePatchLine[] lines =
        [
            Modified(1, "one", "ONE", "\r\n"),
            Modified(3, "three", "THREE", "\r\n"),
        ];

        await service.StageHunkAsync(
            repository.Path, path, "Unstaged", lines, CancellationToken.None);
        await AssertIndexAsync(repository, path, ChangedContent(path));

        await service.UnstageHunkAsync(
            repository.Path, path, lines, CancellationToken.None);
        await AssertIndexAsync(repository, path, BaselineContent(path));
    }

    private static async Task PrepareFilesAsync(TestRepository repository)
    {
        await repository.RunGitAsync("config", "core.autocrlf", "false");
        foreach (var path in Paths) await WriteAsync(repository, path, BaselineContent(path));
        await repository.RunGitAsync("add", "--", "crlf.txt", "no-final-newline.txt", "mixed.txt");
        await repository.RunGitAsync("commit", "-m", "line-ending baselines");
        foreach (var path in Paths) await WriteAsync(repository, path, ChangedContent(path));
    }

    private static Task WriteAsync(TestRepository repository, string path, string content) =>
        File.WriteAllTextAsync(Path.Combine(repository.Path, path), content);

    private static WorkingTreePatchLine Modified(
        int line,
        string oldText,
        string newText,
        string ending) => new()
        {
            ChangeType = "Modified",
            OldLineNumber = line,
            NewLineNumber = line,
            OldText = oldText,
            NewText = newText,
            OldLineEnding = ending,
            NewLineEnding = ending,
        };

    private static async Task AssertIndexAsync(
        TestRepository repository,
        string path,
        string expectedContent)
    {
        var entries = await new GitIndexReader().ReadEntriesForPathAsync(
            Path.Combine(repository.Path, ".git"), GitObjectFormat.Sha1,
            path, CancellationToken.None);
        Assert.Equal(BlobId(expectedContent), Assert.Single(entries).ObjectId.Value);
    }

    private static string BlobId(string content)
    {
        var bytes = Encoding.UTF8.GetBytes(content);
        var header = Encoding.ASCII.GetBytes($"blob {bytes.Length}\0");
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA1);
        hash.AppendData(header);
        hash.AppendData(bytes);
        return Convert.ToHexString(hash.GetHashAndReset()).ToLowerInvariant();
    }

    private static string BaselineContent(string path) => path switch
    {
        "crlf.txt" => "one\r\ntwo\r\n",
        "no-final-newline.txt" => "one\nlast",
        _ => "one\r\ntwo\nthree\r\n",
    };

    private static string ChangedContent(string path) => path switch
    {
        "crlf.txt" => "ONE\r\ntwo\r\n",
        "no-final-newline.txt" => "one\nLAST",
        _ => "ONE\r\ntwo\nTHREE\r\n",
    };

    private static readonly string[] Paths = ["crlf.txt", "no-final-newline.txt", "mixed.txt"];
}
