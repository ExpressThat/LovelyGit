using ExpressThat.LovelyGit.Services.Git.Cli;

namespace LovelyGit.Tests.Git.Cli;

public sealed class GitCliInstallationTests
{
    [Fact]
    public void ResolvePathGitInstallation_PrefersDirectWindowsGitAndPreservesHelperPaths()
    {
        using var installation = TemporaryGitInstallation.Create();

        var result = GitCliService.ResolvePathGitInstallation(
            GitCliOperatingSystem.Windows,
            installation.CmdDirectory);

        Assert.NotNull(result);
        Assert.Equal(installation.RootDirectory, result.RootDirectory);
        Assert.Equal(installation.DirectGitPath, result.GitExecutablePath);
        Assert.Contains(installation.CmdDirectory, result.PathDirectories);
        Assert.Contains(installation.DirectGitDirectory, result.PathDirectories);
        Assert.Contains(installation.UsrBinDirectory, result.PathDirectories);
    }

    private sealed class TemporaryGitInstallation : IDisposable
    {
        private readonly DirectoryInfo _directory;

        private TemporaryGitInstallation(DirectoryInfo directory)
        {
            _directory = directory;
            RootDirectory = directory.FullName;
            CmdDirectory = Path.Combine(RootDirectory, "cmd");
            DirectGitDirectory = Path.Combine(RootDirectory, "mingw64", "bin");
            DirectGitPath = Path.Combine(DirectGitDirectory, "git.exe");
            UsrBinDirectory = Path.Combine(RootDirectory, "usr", "bin");
        }

        public string CmdDirectory { get; }
        public string DirectGitDirectory { get; }
        public string DirectGitPath { get; }
        public string RootDirectory { get; }
        public string UsrBinDirectory { get; }

        public static TemporaryGitInstallation Create()
        {
            var directory = Directory.CreateTempSubdirectory("lovelygit-git-installation-");
            var installation = new TemporaryGitInstallation(directory);
            Directory.CreateDirectory(installation.CmdDirectory);
            Directory.CreateDirectory(installation.DirectGitDirectory);
            Directory.CreateDirectory(installation.UsrBinDirectory);
            File.WriteAllBytes(Path.Combine(installation.CmdDirectory, "git.exe"), []);
            File.WriteAllBytes(installation.DirectGitPath, []);
            return installation;
        }

        public void Dispose() => _directory.Delete(recursive: true);
    }
}
