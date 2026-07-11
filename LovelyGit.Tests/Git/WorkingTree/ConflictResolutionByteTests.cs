using ExpressThat.LovelyGit.Services.Git.CommitGraph.Models;
using ExpressThat.LovelyGit.Services.Git.WorkingTree;
using LovelyGit.Tests.Git.RepositoryOperations;

namespace LovelyGit.Tests.Git.WorkingTree;

public sealed class ConflictResolutionByteTests
{
    [Theory]
    [InlineData("current\r\nincoming")]
    [InlineData("current\nincoming\n")]
    public async Task ResolveAsync_WritesExactUtf8BytesIncludingLineEndings(string resultText)
    {
        using var repository = await CreateConflictAsync();
        var service = new ConflictResolutionService(new WorkingTreeIndexService(repository.Git));
        var opened = await service.ReadAsync(
            repository.Path,
            "shared.txt",
            CommitDiffViewMode.SideBySide,
            ignoreWhitespace: false,
            CancellationToken.None);

        await service.ResolveAsync(
            repository.Path,
            "shared.txt",
            opened.WorktreeFingerprint,
            resultText,
            source: null,
            deleteResult: false,
            CancellationToken.None);

        Assert.Equal(
            System.Text.Encoding.UTF8.GetBytes(resultText),
            await File.ReadAllBytesAsync(Path.Combine(repository.Path, "shared.txt")));
        Assert.Equal(string.Empty, (await repository.Git.ExecuteBufferedAsync(
            ["ls-files", "--unmerged"],
            repository.Path)).StandardOutput);
    }

    private static async Task<TestRepository> CreateConflictAsync()
    {
        var repository = TestRepository.Create();
        await repository.CreateBranchCommitAsync("conflict", "shared.txt", "incoming");
        await repository.SwitchAsync("main");
        await repository.CommitFileAsync("shared.txt", "current", "current conflict");
        Assert.False((await repository.Service.MergeAsync(
            repository.Path,
            "conflict",
            CancellationToken.None)).IsCompleted);
        return repository;
    }
}
