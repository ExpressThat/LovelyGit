using ExpressThat.LovelyGit.Services.Git.CommitGraph;
using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser;
using LovelyGit.Tests.Git.Branches;

namespace LovelyGit.Tests.Git.CommitGraph;

public sealed class CommitPatchLimitTests
{
    [Fact]
    public async Task OversizedSingleFile_ReturnsOnlyTruncationMetadata()
    {
        using var repository = TemporaryGitRepository.Create();
        await WriteAndCommitAsync(repository, "large.txt", new string('a', 2_100_000), "Add large file");
        var commit = await WriteAndCommitAsync(
            repository, "large.txt", new string('b', 2_100_000), "Replace large file");

        var response = await new CommitPatchService().GetCommitPatchAsync(
            repository.Path, GitObjectId.Parse(commit), CancellationToken.None);

        Assert.True(response.IsTruncated);
        Assert.Empty(response.Patch);
    }

    [Fact]
    public async Task TooManyChangedFiles_SkipsUnusablePartialPatch()
    {
        using var repository = TemporaryGitRepository.Create();
        for (var index = 0; index < 201; index++)
        {
            await File.WriteAllTextAsync(
                Path.Combine(repository.Path, $"file-{index:D3}.txt"), $"file {index}\n");
        }
        var commit = await CommitAsync(repository, "Add too many files");

        var response = await new CommitPatchService().GetCommitPatchAsync(
            repository.Path, GitObjectId.Parse(commit), CancellationToken.None);

        Assert.True(response.IsTruncated);
        Assert.Empty(response.Patch);
    }

    private static async Task<string> WriteAndCommitAsync(
        TemporaryGitRepository repository,
        string path,
        string content,
        string message)
    {
        await File.WriteAllTextAsync(Path.Combine(repository.Path, path), content);
        return await CommitAsync(repository, message);
    }

    private static async Task<string> CommitAsync(
        TemporaryGitRepository repository,
        string message)
    {
        await repository.GitCliService.ExecuteBufferedAsync(["add", "--all"], repository.Path);
        await repository.GitCliService.ExecuteBufferedAsync(["commit", "-m", message], repository.Path);
        var result = await repository.GitCliService.ExecuteBufferedAsync(
            ["rev-parse", "HEAD"], repository.Path);
        return result.StandardOutput.Trim();
    }
}
