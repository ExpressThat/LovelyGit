using ExpressThat.LovelyGit.Services.Git.Cli;

namespace LovelyGit.Tests.Git.Cli;

internal sealed class TemporaryRemoteGitRepository : IDisposable
{
    private static readonly string[] SeedFileNames =
    [
        "readme.txt", "local.txt", "remote.txt", "first.txt", "replacement.txt",
        "ahead.txt", "behind.txt", "diverged.txt",
    ];
    private static readonly RepositoryTemplate<string> Template = new(
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

    public string Head(string path)
    {
        var gitDirectory = Directory.Exists(Path.Combine(path, ".git"))
            ? Path.Combine(path, ".git")
            : path;
        var head = File.ReadAllText(Path.Combine(gitDirectory, "HEAD")).Trim();
        const string prefix = "ref:";
        return head.StartsWith(prefix, StringComparison.Ordinal)
            ? ReadRef(gitDirectory, head[prefix.Length..].Trim())
            : head;
    }

    public string[] RemoteNames() =>
        ReadRemotes().Keys.Order(StringComparer.Ordinal).ToArray();

    public string RemoteUrl(string name, bool push = false) =>
        ReadRemotes()[name] is var remote && push && remote.PushUrl != null
            ? remote.PushUrl
            : remote.Url;

    public string CreateBareRemoteCopy(string name, string branchName)
    {
        var path = Path.Combine(_directory.FullName, $"{name}.git");
        CopyDirectory(new DirectoryInfo(BarePath), Directory.CreateDirectory(path));
        WriteRef(path, $"refs/heads/{branchName}", Head(ClonePath));
        return path;
    }

    public void AddRemoteConfig(string name, string url) => File.AppendAllText(
        Path.Combine(ClonePath, ".git", "config"),
        $"\n[remote \"{name}\"]\n\turl = {EscapeConfigValue(url)}\n" +
        $"\tfetch = +refs/heads/*:refs/remotes/{name}/*\n");

    public void WriteRemoteTrackingRef(string name, string branchName) =>
        WriteRef(
            Path.Combine(ClonePath, ".git"),
            $"refs/remotes/{name}/{branchName}",
            Head(ClonePath));

    public bool HasRef(string path, string refName)
    {
        var gitDirectory = Directory.Exists(Path.Combine(path, ".git"))
            ? Path.Combine(path, ".git")
            : path;
        var loosePath = Path.Combine(
            gitDirectory,
            refName.Replace('/', Path.DirectorySeparatorChar));
        return File.Exists(loosePath) ||
               File.Exists(Path.Combine(gitDirectory, "packed-refs")) &&
               File.ReadLines(Path.Combine(gitDirectory, "packed-refs"))
                   .Any(line => line.EndsWith($" {refName}", StringComparison.Ordinal));
    }

    public void Commit(string path, string fileName, string message)
    {
        File.WriteAllText(Path.Combine(path, fileName), message);
        RunGit(path, ["commit", "--all", "-m", message]);
    }

    public void CommitAndPushFromUpdater(string fileName, string message)
    {
        Commit(UpdaterPath, fileName, message);
        RunGit(UpdaterPath, ["push", "origin", "HEAD"]);
    }

    public static TemporaryRemoteGitRepository Create()
    {
        var (directory, templateRoot) = Template.CreateCopy("lovelygit-remote-");
        var repository = new TemporaryRemoteGitRepository(directory);

        RemoteRepositoryTemplate.RetargetConfig(
            repository.ClonePath, templateRoot, directory.FullName);
        RemoteRepositoryTemplate.RetargetConfig(
            repository.UpdaterPath, templateRoot, directory.FullName);
        return repository;
    }

    private static string InitializeTemplate(DirectoryInfo directory)
    {
        var repository = new TemporaryRemoteGitRepository(directory);

        repository.RunGit(directory.FullName, ["init", "--bare", repository.BarePath]);
        repository.RunGit(directory.FullName, ["init", repository.UpdaterPath]);
        repository.ConfigureIdentity(repository.UpdaterPath);
        foreach (var fileName in SeedFileNames)
        {
            File.WriteAllText(Path.Combine(repository.UpdaterPath, fileName), "initial");
        }
        repository.RunGit(repository.UpdaterPath, ["add", "."]);
        repository.RunGit(repository.UpdaterPath, ["commit", "-m", "initial"]);
        var branch = repository.RunGit(repository.UpdaterPath, ["branch", "--show-current"])
            .StandardOutput.Trim();
        repository.RunGit(
            repository.UpdaterPath,
            ["remote", "add", "origin", repository.BarePath]);
        repository.RunGit(repository.UpdaterPath, ["push", "-u", "origin", branch]);
        repository.RunGit(directory.FullName, ["clone", repository.BarePath, repository.ClonePath]);
        repository.ConfigureIdentity(repository.ClonePath);

        return directory.FullName;
    }

    public void Dispose() => RepositoryTemplateLifetime.DeleteDirectory(_directory);

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

    private static string ReadRef(string gitDirectory, string refName)
    {
        var loosePath = Path.Combine(
            gitDirectory,
            refName.Replace('/', Path.DirectorySeparatorChar));
        if (File.Exists(loosePath)) return File.ReadAllText(loosePath).Trim();
        foreach (var line in File.ReadLines(Path.Combine(gitDirectory, "packed-refs")))
        {
            if (line.EndsWith($" {refName}", StringComparison.Ordinal))
            {
                return line[..line.IndexOf(' ')];
            }
        }
        throw new InvalidOperationException($"Ref {refName} was not found.");
    }

    private static void WriteRef(string gitDirectory, string refName, string hash)
    {
        var path = Path.Combine(gitDirectory, refName.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, hash + "\n");
    }

    private static string EscapeConfigValue(string value) =>
        value.Replace("\\", "\\\\", StringComparison.Ordinal);

    private static void CopyDirectory(DirectoryInfo source, DirectoryInfo destination)
    {
        foreach (var directory in source.EnumerateDirectories("*", SearchOption.AllDirectories))
        {
            Directory.CreateDirectory(Path.Combine(
                destination.FullName,
                Path.GetRelativePath(source.FullName, directory.FullName)));
        }
        foreach (var file in source.EnumerateFiles("*", SearchOption.AllDirectories))
        {
            file.CopyTo(Path.Combine(
                destination.FullName,
                Path.GetRelativePath(source.FullName, file.FullName)));
        }
    }

    private Dictionary<string, RemoteConfiguration> ReadRemotes()
    {
        var remotes = new Dictionary<string, RemoteConfiguration>(StringComparer.Ordinal);
        string? currentName = null;
        foreach (var rawLine in File.ReadLines(Path.Combine(ClonePath, ".git", "config")))
        {
            var line = rawLine.Trim();
            if (line.StartsWith("[remote \"", StringComparison.Ordinal) && line.EndsWith("\"]"))
            {
                currentName = line[9..^2];
                remotes.TryAdd(currentName, new RemoteConfiguration(string.Empty, null));
                continue;
            }
            if (line.Length > 0 && line[0] == '[')
            {
                currentName = null;
                continue;
            }
            if (currentName == null) continue;
            var separator = line.IndexOf('=');
            if (separator < 0) continue;
            var key = line[..separator].Trim();
            var value = line[(separator + 1)..].Trim().Replace("\\\\", "\\");
            var current = remotes[currentName];
            remotes[currentName] = key switch
            {
                "url" => current with { Url = value },
                "pushurl" => current with { PushUrl = value },
                _ => current,
            };
        }
        return remotes;
    }

    private sealed record RemoteConfiguration(string Url, string? PushUrl);
}
