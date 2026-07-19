using ExpressThat.LovelyGit.Services.Git.Cli;

namespace LovelyGit.Tests.Git.Branches;

internal sealed class TemporaryGitRepository : IDisposable
{
    private static readonly RepositoryTemplate<(string Branch, string Head)> Template = new(
        "lovelygit-branch-template-",
        InitializeTemplate);
    private readonly DirectoryInfo _directory;
    private readonly string _defaultBranchName;

    private TemporaryGitRepository(
        DirectoryInfo directory,
        GitCliService gitCliService,
        string defaultBranchName,
        string headCommitHash)
    {
        _directory = directory;
        _defaultBranchName = defaultBranchName;
        GitCliService = gitCliService;
        HeadCommitHash = headCommitHash;
        Path = directory.FullName;
    }

    public GitCliService GitCliService { get; }
    public string HeadCommitHash { get; }
    public string Path { get; }

    public async Task CreateUnmergedBranchAsync(string branchName)
    {
        RunGit(GitCliService, Path, ["checkout", "-b", branchName]);
        RunGit(GitCliService, Path, ["commit", "--allow-empty", "-m", "Unmerged"]);
        await GitCliService.ExecuteBufferedAsync(
            ["checkout", _defaultBranchName],
            Path,
            cancellationToken: CancellationToken.None);
    }

    public static TemporaryGitRepository Create()
    {
        var (directory, state) = Template.CreateCopy("lovelygit-branch-");
        return CreateFromCopy(directory, state.Head, state.Branch);
    }

    internal static TemporaryGitRepository CreateFromCopy(
        DirectoryInfo directory,
        string headCommitHash,
        string defaultBranchName = "master")
    {
        var isolatedGlobalConfig = System.IO.Path.Combine(directory.FullName, "global.gitconfig");
        var gitCliService = new GitCliService(new Dictionary<string, string?>
        {
            ["GIT_CONFIG_GLOBAL"] = isolatedGlobalConfig,
        });
        return new TemporaryGitRepository(
            directory, gitCliService, defaultBranchName, headCommitHash);
    }

    private static (string Branch, string Head) InitializeTemplate(DirectoryInfo directory)
    {
        var isolatedGlobalConfig = System.IO.Path.Combine(directory.FullName, "global.gitconfig");
        File.WriteAllText(isolatedGlobalConfig, string.Empty);
        var head = InitializedRepositoryTemplate.CopyInto(directory, "master");
        return ("master", head);
    }

    public void Dispose() => TemporaryGitDirectory.Delete(_directory);

    private static CliWrap.Buffered.BufferedCommandResult RunGit(
        GitCliService gitCliService,
        string workingDirectory,
        IReadOnlyList<string> arguments) =>
        gitCliService.ExecuteBufferedAsync(arguments, workingDirectory).GetAwaiter().GetResult();
}

internal sealed class TemporaryBareGitRepository : IDisposable
{
    private readonly DirectoryInfo _directory;

    private TemporaryBareGitRepository(DirectoryInfo directory)
    {
        _directory = directory;
        Path = directory.FullName;
    }

    public string Path { get; }

    public static TemporaryBareGitRepository Create(GitCliService gitCliService)
    {
        var directory = Directory.CreateTempSubdirectory("lovelygit-remote-");
        gitCliService.ExecuteBufferedAsync(["init", "--bare"], directory.FullName)
            .GetAwaiter()
            .GetResult();
        return new TemporaryBareGitRepository(directory);
    }

    public void Dispose() => TemporaryGitDirectory.Delete(_directory);
}

internal static class TemporaryGitDirectory
{
    public static void Delete(DirectoryInfo directory) =>
        RepositoryTemplateLifetime.DeleteDirectory(directory);
}
