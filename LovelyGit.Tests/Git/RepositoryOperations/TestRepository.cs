using ExpressThat.LovelyGit.Services.Git.Cli;

namespace LovelyGit.Tests.Git.RepositoryOperations;

internal sealed class TestRepository : IDisposable
{
    private static readonly RepositoryTemplate<bool> Template = new(
        "lovelygit-integration-template-",
        InitializeTemplate);
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
        var (directory, _) = Template.CreateCopy("lovelygit-integration-");
        return new TestRepository(directory, new GitCliService());
    }

    private static bool InitializeTemplate(DirectoryInfo directory)
    {
        var repository = new TestRepository(directory, new GitCliService());
        repository.RunAsync("init", "--initial-branch=main").GetAwaiter().GetResult();
        repository.RunAsync("config", "user.name", "LovelyGit Test").GetAwaiter().GetResult();
        repository.RunAsync("config", "user.email", "test@example.invalid").GetAwaiter().GetResult();
        File.WriteAllText(System.IO.Path.Combine(repository.Path, "shared.txt"), "base");
        repository.RunAsync("add", ".").GetAwaiter().GetResult();
        repository.RunAsync("commit", "-m", "initial").GetAwaiter().GetResult();
        return true;
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
        var filePath = System.IO.Path.Combine(Path, relativePath);
        var wasTracked = File.Exists(filePath);
        await File.WriteAllTextAsync(filePath, content);

        if (wasTracked)
        {
            await RunAsync("commit", "-m", message, "--only", "--", relativePath);
            return;
        }

        await RunAsync("add", "--", relativePath);
        await RunAsync("commit", "-m", message);
    }

    public async Task CreateBranchCommitAsync(
        string branchName,
        string relativePath,
        string content)
    {
        var head = await GetHeadHashAsync();
        await GitFastImportFixtureSeeder.SeedLinearCommitsAsync(
            Path,
            $"refs/heads/{branchName}",
            head,
            [new(
                "2024-01-01T00:00:00Z",
                $"{branchName} change",
                Path: relativePath,
                Content: content)]);
        await RunAsync("switch", branchName);
    }

    public Task SwitchAsync(string branchName) => RunAsync("switch", branchName);

    public async Task<string> GetHeadHashAsync()
    {
        var gitDirectory = System.IO.Path.Combine(Path, ".git");
        var head = (await File.ReadAllTextAsync(
            System.IO.Path.Combine(gitDirectory, "HEAD"))).Trim();
        const string refPrefix = "ref:";
        if (!head.StartsWith(refPrefix, StringComparison.Ordinal))
        {
            return head;
        }

        var refName = head[refPrefix.Length..].Trim();
        return (await File.ReadAllTextAsync(System.IO.Path.Combine(
            gitDirectory,
            refName.Replace('/', System.IO.Path.DirectorySeparatorChar)))).Trim();
    }

    public Task RunGitAsync(params string[] arguments) => RunAsync(arguments);

    public void Dispose() => RepositoryTemplateLifetime.DeleteDirectory(_directory);

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
