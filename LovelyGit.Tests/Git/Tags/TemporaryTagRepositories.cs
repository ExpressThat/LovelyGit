using ExpressThat.LovelyGit.Services.Git.Cli;

namespace LovelyGit.Tests.Git.Tags;

internal sealed class TemporaryBareRepository : IDisposable
{
    private readonly DirectoryInfo _directory;

    private TemporaryBareRepository(DirectoryInfo directory)
    {
        _directory = directory;
        Path = directory.FullName;
    }

    public string Path { get; }

    public static TemporaryBareRepository Create()
    {
        var directory = Directory.CreateTempSubdirectory("lovelygit-tag-remote-");
        new GitCliService()
            .ExecuteBufferedAsync(["init", "--bare"], directory.FullName)
            .GetAwaiter()
            .GetResult();
        return new TemporaryBareRepository(directory);
    }

    public void Dispose() => DeleteDirectory(_directory);

    internal static void DeleteDirectory(DirectoryInfo directory)
    {
        foreach (var file in directory.EnumerateFiles("*", SearchOption.AllDirectories))
        {
            file.Attributes = FileAttributes.Normal;
        }
        directory.Delete(recursive: true);
    }
}

internal sealed class TemporaryTagGitRepository : IDisposable
{
    private static readonly RepositoryTemplate<string> Template = new(
        "lovelygit-tag-template-",
        InitializeTemplate);
    private readonly DirectoryInfo _directory;

    private TemporaryTagGitRepository(
        DirectoryInfo directory,
        GitCliService gitCliService,
        string headCommitHash)
    {
        _directory = directory;
        GitCliService = gitCliService;
        GitOperationService = new GitOperationService(gitCliService);
        HeadCommitHash = headCommitHash;
        Path = directory.FullName;
    }

    public GitCliService GitCliService { get; }

    public GitOperationService GitOperationService { get; }

    public string HeadCommitHash { get; }

    public string Path { get; }

    public static TemporaryTagGitRepository Create()
    {
        var (directory, head) = Template.CreateCopy("lovelygit-tag-");
        var gitCliService = new GitCliService();
        return new TemporaryTagGitRepository(directory, gitCliService, head);
    }

    private static string InitializeTemplate(DirectoryInfo directory)
    {
        var gitCliService = new GitCliService();
        InitializedRepositoryTemplate.CopyInto(directory, "master");
        var head = RunGit(gitCliService, directory.FullName, ["rev-parse", "HEAD"])
            .StandardOutput.Trim();
        return head;
    }

    public async Task<string> ResolveHeadAsync()
    {
        var result = await GitCliService.ExecuteBufferedAsync(
            ["rev-parse", "HEAD"],
            Path,
            cancellationToken: CancellationToken.None);
        return result.StandardOutput.Trim();
    }

    public void Dispose() => TemporaryBareRepository.DeleteDirectory(_directory);

    private static CliWrap.Buffered.BufferedCommandResult RunGit(
        GitCliService gitCliService,
        string workingDirectory,
        IReadOnlyList<string> arguments) =>
        gitCliService.ExecuteBufferedAsync(arguments, workingDirectory)
            .GetAwaiter().GetResult();
}
