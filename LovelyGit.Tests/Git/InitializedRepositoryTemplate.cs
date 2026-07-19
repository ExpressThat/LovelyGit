using ExpressThat.LovelyGit.Services.Git.Cli;

namespace LovelyGit.Tests.Git;

internal static class InitializedRepositoryTemplate
{
    private static readonly RepositoryTemplate<string> Main = new(
        "lovelygit-main-base-template-",
        directory => Initialize(directory, "main"));
    private static readonly RepositoryTemplate<string> Master = new(
        "lovelygit-master-base-template-",
        directory => Initialize(directory, "master"));

    public static string CopyInto(DirectoryInfo directory, string branchName = "main")
    {
        var template = branchName.Equals("master", StringComparison.Ordinal)
            ? Master
            : Main;
        return template.CopyInto(directory);
    }

    private static string Initialize(DirectoryInfo directory, string branchName)
    {
        var git = new GitCliService();
        Run(git, directory, ["init", "--initial-branch", branchName]);
        Run(git, directory, ["config", "user.name", "LovelyGit Test"]);
        Run(git, directory, ["config", "user.email", "test@example.invalid"]);
        Run(git, directory, ["config", "core.autocrlf", "false"]);
        Run(git, directory, ["commit", "--allow-empty", "-m", "Initial"]);
        return Run(git, directory, ["rev-parse", "HEAD"]).Trim();
    }

    private static string Run(
        GitCliService git,
        DirectoryInfo directory,
        IReadOnlyList<string> arguments) =>
        git.ExecuteBufferedAsync(arguments, directory.FullName)
            .GetAwaiter().GetResult().StandardOutput;
}
