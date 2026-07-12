using ExpressThat.LovelyGit.Services.Git.Cli;

namespace LovelyGit.Tests.Git.Stashes;

internal sealed class TemporaryGitRepository : IDisposable
{
    private static readonly RepositoryTemplate<bool> Template = new(
        "lovelygit-stash-template-",
        InitializeTemplate);
    private readonly DirectoryInfo _directory;

    private TemporaryGitRepository(
        DirectoryInfo directory,
        GitCliService gitCliService)
    {
        _directory = directory;
        GitCliService = gitCliService;
        GitOperationService = new GitOperationService(gitCliService);
        Path = directory.FullName;
    }

    public GitCliService GitCliService { get; }

    public GitOperationService GitOperationService { get; }

    public string Path { get; }

    public string TrackedPath => System.IO.Path.Combine(Path, "tracked.txt");

    public static TemporaryGitRepository Create()
    {
        var (directory, _) = Template.CreateCopy("lovelygit-stash-");
        var gitCliService = new GitCliService();

        return new TemporaryGitRepository(directory, gitCliService);
    }

    private static bool InitializeTemplate(DirectoryInfo directory)
    {
        var gitCliService = new GitCliService();
        InitializedRepositoryTemplate.CopyInto(directory, "master");
        File.WriteAllText(System.IO.Path.Combine(directory.FullName, "tracked.txt"), "tracked");
        RunGit(gitCliService, directory.FullName, ["add", "tracked.txt"]);
        RunGit(gitCliService, directory.FullName, ["commit", "-m", "Initial"]);
        return true;
    }

    public void Dispose()
    {
        foreach (var file in _directory.EnumerateFiles("*", SearchOption.AllDirectories))
        {
            file.Attributes = FileAttributes.Normal;
        }

        _directory.Delete(recursive: true);
    }

    private static CliWrap.Buffered.BufferedCommandResult RunGit(
        GitCliService gitCliService,
        string workingDirectory,
        IReadOnlyList<string> arguments) =>
        gitCliService
            .ExecuteBufferedAsync(arguments, workingDirectory)
            .GetAwaiter()
            .GetResult();
}
