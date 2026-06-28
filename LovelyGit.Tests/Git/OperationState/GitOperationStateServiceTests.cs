using ExpressThat.LovelyGit.Services.Git.OperationState;
using LovelyGit.Tests.Git.WorkingTree;

namespace LovelyGit.Tests.Git.OperationState;

public sealed class GitOperationStateServiceTests
{
    [Fact]
    public async Task GetStateAsync_ReturnsReadyForCleanRepository()
    {
        using var directory = TemporaryDirectory.Create("lovelygit-operation-state-");
        await GitTestProcess.RunAsync(directory.Path, "init");

        var state = await ReadStateAsync(directory.Path);

        Assert.Equal(GitOperationKind.None, state.Kind);
        Assert.False(state.IsInProgress);
    }

    [Theory]
    [InlineData("MERGE_HEAD", GitOperationKind.Merge, "Merge in progress")]
    [InlineData("CHERRY_PICK_HEAD", GitOperationKind.CherryPick, "Cherry-pick in progress")]
    [InlineData("REVERT_HEAD", GitOperationKind.Revert, "Revert in progress")]
    [InlineData("BISECT_LOG", GitOperationKind.Bisect, "Bisect in progress")]
    public async Task GetStateAsync_DetectsOperationMarkerFiles(
        string markerFile,
        GitOperationKind kind,
        string label)
    {
        using var directory = TemporaryDirectory.Create("lovelygit-operation-state-");
        await GitTestProcess.RunAsync(directory.Path, "init");
        await File.WriteAllTextAsync(Path.Combine(directory.Path, ".git", markerFile), "marker");

        var state = await ReadStateAsync(directory.Path);

        Assert.Equal(kind, state.Kind);
        Assert.Equal(label, state.Label);
        Assert.True(state.IsInProgress);
    }

    [Theory]
    [InlineData("rebase-merge")]
    [InlineData("rebase-apply")]
    public async Task GetStateAsync_DetectsRebaseDirectories(string directoryName)
    {
        using var directory = TemporaryDirectory.Create("lovelygit-operation-state-");
        await GitTestProcess.RunAsync(directory.Path, "init");
        Directory.CreateDirectory(Path.Combine(directory.Path, ".git", directoryName));

        var state = await ReadStateAsync(directory.Path);

        Assert.Equal(GitOperationKind.Rebase, state.Kind);
        Assert.Equal("Rebase in progress", state.Label);
    }

    private static async Task<GitOperationState> ReadStateAsync(string path) =>
        await new GitOperationStateService().GetStateAsync(path, CancellationToken.None);
}
