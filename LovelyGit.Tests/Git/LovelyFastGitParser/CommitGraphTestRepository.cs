using ExpressThat.LovelyGit.Services.Git.Cli;

namespace LovelyGit.Tests.Git.LovelyFastGitParser;

internal sealed class CommitGraphTestRepository : IDisposable
{
    private readonly DirectoryInfo _directory;
    private readonly GitCliService _git = new();
    private string? _head;

    private CommitGraphTestRepository(DirectoryInfo directory) => _directory = directory;

    public string Path => _directory.FullName;

    public IReadOnlyList<string> GraphFiles => Directory.Exists(GraphDirectory)
        ? Directory.GetFiles(GraphDirectory, "*.graph")
        : File.Exists(SingleGraph) ? [SingleGraph] : [];

    private string GraphDirectory => System.IO.Path.Combine(
        Path, ".git", "objects", "info", "commit-graphs");

    private string SingleGraph => System.IO.Path.Combine(
        Path, ".git", "objects", "info", "commit-graph");

    public static CommitGraphTestRepository Create(bool sha256 = false)
    {
        var repository = new CommitGraphTestRepository(
            Directory.CreateTempSubdirectory("lovelygit-commit-graph-"));
        if (!sha256)
        {
            repository._head = InitializedRepositoryTemplate.CopyInto(
                repository._directory, "master");
            return repository;
        }

        repository.Run("init", "--object-format=sha256");
        repository.Run("config", "user.name", "LovelyGit Test");
        repository.Run("config", "user.email", "test@example.invalid");
        repository.Run("config", "core.autocrlf", "false");
        return repository;
    }

    public string Commit(string subject, string file)
    {
        if (_head != null)
        {
            _head = GitFastImportFixtureSeeder.SeedLinearFileHistoryAsync(
                Path,
                "refs/heads/master",
                _head,
                file,
                [subject + "\n"]).GetAwaiter().GetResult().Single();
            return _head;
        }

        File.WriteAllText(System.IO.Path.Combine(Path, file), subject + "\n");
        Run("add", file);
        Run("commit", "-m", subject);
        return Run("rev-parse", "HEAD");
    }

    public void WriteGraph() => Run("commit-graph", "write", "--reachable");

    public void AddHistoryOverride(string mode, string head)
    {
        if (mode == "config")
        {
            Run("config", "core.commitGraph", "false");
            return;
        }

        if (mode == "shallow")
        {
            File.WriteAllText(System.IO.Path.Combine(Path, ".git", "shallow"), head + "\n");
            return;
        }

        var directory = Directory.CreateDirectory(
            System.IO.Path.Combine(Path, ".git", "refs", "replace"));
        File.WriteAllText(System.IO.Path.Combine(directory.FullName, head), head + "\n");
    }

    public string Run(params string[] arguments) => Run((IReadOnlyList<string>)arguments);

    private string Run(IReadOnlyList<string> arguments) => _git
        .ExecuteBufferedAsync(arguments, Path).GetAwaiter().GetResult().StandardOutput.Trim();

    public void Dispose()
    {
        foreach (var file in _directory.EnumerateFiles("*", SearchOption.AllDirectories))
        {
            file.Attributes = FileAttributes.Normal;
        }
        _directory.Delete(recursive: true);
    }
}
