using ExpressThat.LovelyGit.Services.Git.Cli;

namespace LovelyGit.Tests.Git.Stashes;

internal sealed class TemporaryGitRepository : IDisposable
{
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
        var directory = Directory.CreateTempSubdirectory("lovelygit-stash-");
        var gitCliService = new GitCliService();

        RunGit(gitCliService, directory.FullName, ["init"]);
        RunGit(gitCliService, directory.FullName, ["config", "user.name", "LovelyGit Test"]);
        RunGit(gitCliService, directory.FullName, ["config", "user.email", "test@example.invalid"]);
        File.WriteAllText(System.IO.Path.Combine(directory.FullName, "tracked.txt"), "tracked");
        RunGit(gitCliService, directory.FullName, ["add", "tracked.txt"]);
        RunGit(gitCliService, directory.FullName, ["commit", "-m", "Initial"]);

        return new TemporaryGitRepository(directory, gitCliService);
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
