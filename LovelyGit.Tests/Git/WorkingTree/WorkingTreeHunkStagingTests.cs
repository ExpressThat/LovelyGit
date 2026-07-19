using ExpressThat.LovelyGit.Services.Git.WorkingTree;
using ExpressThat.LovelyGit.Services.Git.WorkingTree.Models;
using LovelyGit.Tests.Git.RepositoryOperations;

namespace LovelyGit.Tests.Git.WorkingTree;

public sealed class WorkingTreeHunkStagingTests
{
    [Fact]
    public async Task ExistingSingleLineStageAndUnstageBehaviorIsPreserved()
    {
        using var repository = TestRepository.Create();
        const string baseline = "one\ntwo\n";
        const string changed = "ONE\ntwo\n";
        await repository.CommitFileAsync("shared.txt", baseline, "line baseline");
        await File.WriteAllTextAsync(Path.Combine(repository.Path, "shared.txt"), changed);
        var service = new WorkingTreeIndexService(repository.Git);

        await service.StageLineAsync(
            repository.Path,
            "shared.txt",
            "Unstaged",
            "Modified",
            1,
            1,
            "one",
            "ONE",
            null,
            null,
            CancellationToken.None);
        Assert.Equal(changed, await ReadIndexFileAsync(repository));

        await service.UnstageLineAsync(
            repository.Path,
            "shared.txt",
            "Modified",
            1,
            1,
            "one",
            "ONE",
            null,
            null,
            CancellationToken.None);
        Assert.Equal(baseline, await ReadIndexFileAsync(repository));
    }

    [Fact]
    public async Task StageAndUnstageHunk_AtomicallyMovesSeparatedModifications()
    {
        using var repository = TestRepository.Create();
        const string baseline = "one\ntwo\nthree\nfour\nfive\n";
        const string changed = "ONE\ntwo\nthree\nFOUR\nfive\n";
        await repository.CommitFileAsync("shared.txt", baseline, "multi-line baseline");
        await File.WriteAllTextAsync(Path.Combine(repository.Path, "shared.txt"), changed);
        var service = new WorkingTreeIndexService(repository.Git);
        var lines = new[]
        {
            Modified(1, "one", "ONE"),
            Modified(4, "four", "FOUR"),
        };

        await service.StageHunkAsync(
            repository.Path,
            "shared.txt",
            "Unstaged",
            lines,
            CancellationToken.None);

        Assert.Equal(changed, await ReadIndexFileAsync(repository));
        Assert.Equal(changed, await File.ReadAllTextAsync(Path.Combine(repository.Path, "shared.txt")));

        await service.UnstageHunkAsync(
            repository.Path,
            "shared.txt",
            lines,
            CancellationToken.None);

        Assert.Equal(baseline, await ReadIndexFileAsync(repository));
        Assert.Equal(changed, await File.ReadAllTextAsync(Path.Combine(repository.Path, "shared.txt")));
    }

    [Fact]
    public async Task StageHunk_AppliesInsertionsAndDeletionsTogether()
    {
        using var repository = TestRepository.Create();
        const string baseline = "a\nb\nc\nd\n";
        const string changed = "a\nnew\nb\nd\n";
        await repository.CommitFileAsync("shared.txt", baseline, "insert-delete baseline");
        await File.WriteAllTextAsync(Path.Combine(repository.Path, "shared.txt"), changed);
        var service = new WorkingTreeIndexService(repository.Git);
        var lines = new[]
        {
            new WorkingTreePatchLine
            {
                ChangeType = "Inserted",
                NewLineNumber = 2,
                NewText = "new",
            },
            new WorkingTreePatchLine
            {
                ChangeType = "Deleted",
                OldLineNumber = 3,
                OldText = "c",
            },
        };

        await service.StageHunkAsync(
            repository.Path,
            "shared.txt",
            "Unstaged",
            lines,
            CancellationToken.None);

        Assert.Equal(changed, await ReadIndexFileAsync(repository));
    }

    [Fact]
    public async Task StageHunk_InvalidOrCancelledRequestLeavesIndexUnchanged()
    {
        using var repository = TestRepository.Create();
        var service = new WorkingTreeIndexService(repository.Git);
        var before = await ReadIndexFileAsync(repository);
        var invalid = new WorkingTreePatchLine { ChangeType = "Unchanged" };

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.StageHunkAsync(
                repository.Path,
                "shared.txt",
                "Unstaged",
                [invalid],
                CancellationToken.None));
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            service.StageHunkAsync(
                repository.Path,
                "shared.txt",
                "Unstaged",
                [Modified(1, "base", "changed")],
                cancellation.Token));

        Assert.Equal(before, await ReadIndexFileAsync(repository));
    }

    [Fact]
    public async Task FailedUntrackedPatchesRemoveTemporaryIntentToAddEntries()
    {
        using var repository = TestRepository.Create();
        var service = new WorkingTreeIndexService(repository.Git);
        await File.WriteAllTextAsync(Path.Combine(repository.Path, "new.txt"), "actual\n");
        var staleLine = new WorkingTreePatchLine
        {
            ChangeType = "Deleted",
            OldLineNumber = 1,
            OldText = "missing",
        };

        await Assert.ThrowsAnyAsync<Exception>(() =>
            service.StageHunkAsync(
                repository.Path,
                "new.txt",
                "Untracked",
                [staleLine],
                CancellationToken.None));

        var status = await repository.Git.ExecuteBufferedAsync(
            ["status", "--short", "--", "new.txt"],
            repository.Path,
            cancellationToken: CancellationToken.None);
        Assert.Equal("?? new.txt", status.StandardOutput.Trim());

        await Assert.ThrowsAnyAsync<Exception>(() =>
            service.StageLineAsync(
                repository.Path,
                "new.txt",
                "Untracked",
                "Deleted",
                1,
                null,
                "missing",
                string.Empty,
                null,
                null,
                CancellationToken.None));

        status = await repository.Git.ExecuteBufferedAsync(
            ["status", "--short", "--", "new.txt"],
            repository.Path,
            cancellationToken: CancellationToken.None);
        Assert.Equal("?? new.txt", status.StandardOutput.Trim());
    }

    private static WorkingTreePatchLine Modified(int lineNumber, string oldText, string newText) =>
        new()
        {
            ChangeType = "Modified",
            OldLineNumber = lineNumber,
            NewLineNumber = lineNumber,
            OldText = oldText,
            NewText = newText,
        };

    private static async Task<string> ReadIndexFileAsync(TestRepository repository)
    {
        var result = await repository.Git.ExecuteBufferedAsync(
            ["show", ":shared.txt"],
            repository.Path,
            cancellationToken: CancellationToken.None);
        return result.StandardOutput.Replace("\r\n", "\n", StringComparison.Ordinal);
    }
}
