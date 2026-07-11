using LovelyGit.Tests.Git.RepositoryOperations;

namespace LovelyGit.Tests.Git.WorkingTree;

public sealed class ConflictRepositoryFixture : IAsyncLifetime
{
    private TestRepository? _template;

    internal TestRepository CreateCopy() =>
        (_template ?? throw new InvalidOperationException("Fixture is not initialized.")).Copy();

    public async Task InitializeAsync()
    {
        _template = TestRepository.Create();
        await _template.CreateBranchCommitAsync("conflict", "shared.txt", "feature");
        await _template.SwitchAsync("main");
        await _template.CommitFileAsync("shared.txt", "main", "main conflict");
        Assert.False((await _template.Service.MergeAsync(
            _template.Path,
            "conflict",
            CancellationToken.None)).IsCompleted);
    }

    public Task DisposeAsync()
    {
        _template?.Dispose();
        _template = null;
        return Task.CompletedTask;
    }
}
