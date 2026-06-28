using ExpressThat.LovelyGit.Services.Git.Cli;
using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser;

namespace LovelyGit.Tests.Git.LovelyFastGitParser;

public sealed class GitStashRefReaderTests
{
    [Fact]
    public async Task OpenAsync_IncludesStashRefOnStashCommit()
    {
        using var temporary = TemporaryGitRepository.Create();
        using var repository = await LovelyGitRepository.OpenAsync(
            temporary.Path,
            CancellationToken.None);
        var startingCommits = await repository.GetStartingCommitsAsync(CancellationToken.None);

        var stashCommit = Assert.Single(startingCommits, commit =>
            commit.Refs.Any(reference =>
                reference.Kind == GitRefKind.Stash &&
                reference.Name == "stash"));

        Assert.DoesNotContain("stash", stashCommit.Branches);
        Assert.DoesNotContain("stash", stashCommit.Tags);
    }

    [Fact]
    public async Task OpenAsync_IncludesOlderStashReflogEntries()
    {
        using var temporary = TemporaryGitRepository.Create(stashCount: 3);
        using var repository = await LovelyGitRepository.OpenAsync(
            temporary.Path,
            CancellationToken.None);
        var startingCommits = await repository.GetStartingCommitsAsync(CancellationToken.None);
        var stashRefs = startingCommits
            .SelectMany(commit => commit.Refs)
            .Where(reference => reference.Kind == GitRefKind.Stash)
            .Select(reference => reference.Name)
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(["stash", "stash@{1}", "stash@{2}"], stashRefs);
    }

    private sealed class TemporaryGitRepository : IDisposable
    {
        private readonly DirectoryInfo _directory;

        private TemporaryGitRepository(DirectoryInfo directory)
        {
            _directory = directory;
            Path = directory.FullName;
        }

        public string Path { get; }

        public static TemporaryGitRepository Create(int stashCount = 1)
        {
            var directory = Directory.CreateTempSubdirectory("lovelygit-stash-ref-");
            var gitCliService = new GitCliService();

            RunGit(gitCliService, directory.FullName, ["init"]);
            RunGit(gitCliService, directory.FullName, ["config", "user.name", "LovelyGit Test"]);
            RunGit(gitCliService, directory.FullName, ["config", "user.email", "test@example.invalid"]);
            File.WriteAllText(System.IO.Path.Combine(directory.FullName, "tracked.txt"), "tracked");
            RunGit(gitCliService, directory.FullName, ["add", "tracked.txt"]);
            RunGit(gitCliService, directory.FullName, ["commit", "-m", "Initial"]);
            for (var index = 1; index <= stashCount; index++)
            {
                File.AppendAllText(System.IO.Path.Combine(directory.FullName, "tracked.txt"), $"changed {index}");
                RunGit(gitCliService, directory.FullName, ["stash", "push", "-m", $"Parser stash {index}"]);
            }

            return new TemporaryGitRepository(directory);
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
            IReadOnlyList<string> arguments)
        {
            return gitCliService
                .ExecuteBufferedAsync(arguments, workingDirectory)
                .GetAwaiter()
                .GetResult();
        }
    }
}
