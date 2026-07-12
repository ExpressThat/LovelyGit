using ExpressThat.LovelyGit.Services.Git.Cli;

namespace LovelyGit.Tests.Git.RepositoryOperations;

internal sealed class TestRepository : IDisposable
{
    private static readonly Lazy<DirectoryInfo> Template = new(
        CreateTemplate,
        LazyThreadSafetyMode.ExecutionAndPublication);
    private readonly DirectoryInfo _directory;

    private TestRepository(DirectoryInfo directory, GitCliService git)
    {
        _directory = directory;
        Git = git;
        Path = directory.FullName;
        Service = new GitRepositoryOperationService(new GitOperationService(git));
    }

    public GitCliService Git { get; }

    public string Path { get; }

    public GitRepositoryOperationService Service { get; }

    public static TestRepository Create()
    {
        var directory = Directory.CreateTempSubdirectory("lovelygit-integration-");
        CopyDirectory(Template.Value, directory);
        return new TestRepository(directory, new GitCliService());
    }

    private static DirectoryInfo CreateTemplate()
    {
        var directory = Directory.CreateTempSubdirectory("lovelygit-integration-template-");
        var repository = new TestRepository(directory, new GitCliService());
        repository.RunAsync("init", "--initial-branch=main").GetAwaiter().GetResult();
        repository.RunAsync("config", "user.name", "LovelyGit Test").GetAwaiter().GetResult();
        repository.RunAsync("config", "user.email", "test@example.invalid").GetAwaiter().GetResult();
        File.WriteAllText(System.IO.Path.Combine(repository.Path, "shared.txt"), "base");
        repository.RunAsync("add", ".").GetAwaiter().GetResult();
        repository.RunAsync("commit", "-m", "initial").GetAwaiter().GetResult();
        return directory;
    }

    public TestRepository Copy()
    {
        var directory = Directory.CreateTempSubdirectory("lovelygit-integration-copy-");
        CopyDirectory(_directory, directory);
        return new TestRepository(directory, new GitCliService());
    }

    public async Task CommitFileAsync(
        string relativePath,
        string content,
        string message)
    {
        await File.WriteAllTextAsync(System.IO.Path.Combine(Path, relativePath), content);
        await RunAsync("add", "--", relativePath);
        await RunAsync("commit", "-m", message);
    }

    public async Task CreateBranchCommitAsync(
        string branchName,
        string relativePath,
        string content)
    {
        await RunAsync("switch", "--create", branchName);
        await CommitFileAsync(relativePath, content, $"{branchName} change");
    }

    public Task SwitchAsync(string branchName) => RunAsync("switch", branchName);

    public async Task<string> GetHeadHashAsync()
    {
        var result = await Git.ExecuteBufferedAsync(
            ["rev-parse", "HEAD"],
            Path,
            cancellationToken: CancellationToken.None);
        return result.StandardOutput.Trim();
    }

    public Task RunGitAsync(params string[] arguments) => RunAsync(arguments);

    public void Dispose()
    {
        foreach (var file in _directory.EnumerateFiles("*", SearchOption.AllDirectories))
        {
            file.Attributes = FileAttributes.Normal;
        }

        _directory.Delete(recursive: true);
    }

    private async Task RunAsync(params string[] arguments)
    {
        await Git.ExecuteBufferedAsync(
            arguments,
            Path,
            cancellationToken: CancellationToken.None);
    }

    private static void CopyDirectory(DirectoryInfo source, DirectoryInfo destination)
    {
        foreach (var directory in source.EnumerateDirectories("*", SearchOption.AllDirectories))
        {
            Directory.CreateDirectory(System.IO.Path.Combine(
                destination.FullName,
                System.IO.Path.GetRelativePath(source.FullName, directory.FullName)));
        }

        foreach (var file in source.EnumerateFiles("*", SearchOption.AllDirectories))
        {
            var target = System.IO.Path.Combine(
                destination.FullName,
                System.IO.Path.GetRelativePath(source.FullName, file.FullName));
            file.CopyTo(target);
        }
    }
}
