using ExpressThat.LovelyGit.Services.Git.Cli;
using ExpressThat.LovelyGit.Services.Git.CommitGraph;
using ExpressThat.LovelyGit.Services.Git.CommitGraph.Models;
using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser;

namespace LovelyGit.Tests.Git.CommitGraph;

internal sealed class FakeCache : ICommitFileDiffCache
{
    public Func<CommitFileDiffResponse?> Get { get; set; } = static () => null;
    public Func<CommitFileDiffResponse, CancellationToken, Task> Save { get; set; } =
        static (_, _) => Task.CompletedTask;

    public Task<CommitFileDiffResponse?> GetAsync(
        Guid repositoryId,
        string commitHash,
        string path,
        CommitDiffViewMode viewMode,
        bool ignoreWhitespace,
        CancellationToken cancellationToken) => Task.FromResult(Get());

    public Task<bool> HasAsync(
        Guid repositoryId,
        string commitHash,
        string path,
        CommitDiffViewMode viewMode,
        bool ignoreWhitespace,
        CancellationToken cancellationToken) => Task.FromResult(Get() is not null);

    public Task SaveAsync(
        Guid repositoryId,
        string commitHash,
        string path,
        CommitFileDiffResponse response,
        bool ignoreWhitespace,
        CancellationToken cancellationToken) => Save(response, cancellationToken);

    public Task RemoveAsync(
        Guid repositoryId,
        string commitHash,
        string path,
        CommitDiffViewMode viewMode,
        bool ignoreWhitespace,
        CancellationToken cancellationToken) => Task.CompletedTask;

    public Task ClearAsync(
        Guid repositoryId,
        string commitHash,
        CancellationToken cancellationToken) => Task.CompletedTask;
}

internal sealed class RepositoryFixture : IDisposable
{
    private static readonly RepositoryTemplate<GitObjectId> Template = new(
        "lovelygit-file-diff-persistence-template-",
        Initialize);
    private readonly DirectoryInfo _directory;

    private RepositoryFixture(DirectoryInfo directory, GitObjectId commitId)
    {
        _directory = directory;
        CommitId = commitId;
    }

    public string Path => _directory.FullName;
    public GitObjectId CommitId { get; }

    public static RepositoryFixture Create()
    {
        var (directory, commitId) = Template.CreateCopy("lovelygit-file-diff-persistence-");
        return new RepositoryFixture(directory, commitId);
    }

    public void Dispose()
    {
        foreach (var file in _directory.EnumerateFiles("*", SearchOption.AllDirectories))
        {
            file.Attributes = FileAttributes.Normal;
        }

        _directory.Delete(true);
    }

    private static GitObjectId Initialize(DirectoryInfo directory)
    {
        InitializedRepositoryTemplate.CopyInto(directory);
        File.WriteAllText(System.IO.Path.Combine(directory.FullName, "sample.txt"), "before\n");
        Run(directory, ["add", "sample.txt"]);
        Run(directory, ["commit", "-m", "Add sample"]);
        File.WriteAllText(System.IO.Path.Combine(directory.FullName, "sample.txt"), "after\nadded\n");
        Run(directory, ["add", "sample.txt"]);
        Run(directory, ["commit", "-m", "Change sample"]);
        return GitObjectId.Parse(Run(directory, ["rev-parse", "HEAD"]).StandardOutput.Trim());
    }

    private static CliWrap.Buffered.BufferedCommandResult Run(
        DirectoryInfo directory,
        IReadOnlyList<string> arguments) =>
        new GitCliService().ExecuteBufferedAsync(arguments, directory.FullName)
            .GetAwaiter().GetResult();
}
