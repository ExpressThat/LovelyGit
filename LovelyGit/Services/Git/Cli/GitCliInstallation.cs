namespace ExpressThat.LovelyGit.Services.Git.Cli;

internal enum GitCliOperatingSystem
{
    Windows,
    Linux,
    MacOs,
}

internal sealed record GitCliInstallation(
    GitCliOperatingSystem OperatingSystem,
    string RootDirectory,
    string GitExecutablePath,
    IReadOnlyList<string> PathDirectories);
