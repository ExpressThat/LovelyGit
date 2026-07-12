using ExpressThat.LovelyGit.Services.Git.Cli;

namespace LovelyGit.Tests.Git;

internal static class InitializedRepositoryTemplate
{
    private static readonly RepositoryTemplate<bool> Main = new(
        "lovelygit-main-base-template-",
        directory => Initialize(directory, "main"));
    private static readonly RepositoryTemplate<bool> Master = new(
        "lovelygit-master-base-template-",
        directory => Initialize(directory, "master"));

    public static void CopyInto(DirectoryInfo directory, string branchName = "main")
    {
        var template = branchName.Equals("master", StringComparison.Ordinal)
            ? Master
            : Main;
        template.CopyInto(directory);
    }

    private static bool Initialize(DirectoryInfo directory, string branchName)
    {
        var git = new GitCliService();
        Run(git, directory, ["init", "--initial-branch", branchName]);
        Run(git, directory, ["config", "user.name", "LovelyGit Test"]);
        Run(git, directory, ["config", "user.email", "test@example.invalid"]);
        Run(git, directory, ["commit", "--allow-empty", "-m", "Initial"]);
        return true;
    }

    private static void Run(
        GitCliService git,
        DirectoryInfo directory,
        IReadOnlyList<string> arguments) =>
        git.ExecuteBufferedAsync(arguments, directory.FullName).GetAwaiter().GetResult();
}
