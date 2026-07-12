using ExpressThat.LovelyGit.Services.Git.Cli;

namespace LovelyGit.Tests.Git.Cli;

internal sealed class TemporaryRemoteGitRepository : IDisposable
{
    private static readonly RepositoryTemplate<bool> Template = new(
        "lovelygit-remote-template-",
        InitializeTemplate);
    private readonly DirectoryInfo _directory;

    private TemporaryRemoteGitRepository(DirectoryInfo directory)
    {
        _directory = directory;
        GitCliService = new GitCliService();
        BarePath = Path.Combine(directory.FullName, "origin.git");
        ClonePath = Path.Combine(directory.FullName, "clone");
        UpdaterPath = Path.Combine(directory.FullName, "updater");
    }

    public string BarePath { get; }
    public string ClonePath { get; }
    public GitCliService GitCliService { get; }
    private string UpdaterPath { get; }

    public string Head(string path) =>
        RunGit(path, ["rev-parse", "HEAD"]).StandardOutput.Trim();

    public string[] RemoteNames() =>
        RunGit(ClonePath, ["remote"])
            .StandardOutput
            .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);

    public string RemoteUrl(string name, bool push = false) =>
        RunGit(ClonePath, push
                ? ["remote", "get-url", "--push", name]
                : ["remote", "get-url", name])
            .StandardOutput.Trim();

    public void Commit(string path, string fileName, string message)
    {
        File.WriteAllText(Path.Combine(path, fileName), message);
        RunGit(path, ["add", "."]);
        RunGit(path, ["commit", "-m", message]);
    }

    public void CommitAndPushFromUpdater(string fileName, string message)
    {
        Commit(UpdaterPath, fileName, message);
        RunGit(UpdaterPath, ["push", "origin", "HEAD"]);
    }

    public static TemporaryRemoteGitRepository Create()
    {
        var (directory, _) = Template.CreateCopy("lovelygit-remote-");
        var repository = new TemporaryRemoteGitRepository(directory);

        repository.RunGit(repository.ClonePath, ["remote", "set-url", "origin", repository.BarePath]);
        repository.RunGit(repository.UpdaterPath, ["remote", "set-url", "origin", repository.BarePath]);
        return repository;
    }

    private static bool InitializeTemplate(DirectoryInfo directory)
    {
        var repository = new TemporaryRemoteGitRepository(directory);

        repository.RunGit(directory.FullName, ["init", "--bare", repository.BarePath]);
        repository.RunGit(directory.FullName, ["init", repository.UpdaterPath]);
        repository.ConfigureIdentity(repository.UpdaterPath);
        repository.Commit(repository.UpdaterPath, "readme.txt", "initial");
        var branch = repository.RunGit(repository.UpdaterPath, ["branch", "--show-current"])
            .StandardOutput.Trim();
        repository.RunGit(
            repository.UpdaterPath,
            ["remote", "add", "origin", repository.BarePath]);
        repository.RunGit(repository.UpdaterPath, ["push", "-u", "origin", branch]);
        repository.RunGit(directory.FullName, ["clone", repository.BarePath, repository.ClonePath]);
        repository.ConfigureIdentity(repository.ClonePath);

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

    public CliWrap.Buffered.BufferedCommandResult RunGit(
        string workingDirectory,
        IReadOnlyList<string> arguments)
    {
        return GitCliService
            .ExecuteBufferedAsync(arguments, workingDirectory)
            .GetAwaiter()
            .GetResult();
    }

    private void ConfigureIdentity(string path)
    {
        RunGit(path, ["config", "user.name", "LovelyGit Test"]);
        RunGit(path, ["config", "user.email", "test@example.invalid"]);
    }
}
