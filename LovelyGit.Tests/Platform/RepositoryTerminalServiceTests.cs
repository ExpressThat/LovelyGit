using ExpressThat.LovelyGit.Services.Platform;

namespace LovelyGit.Tests.Platform;

public sealed class RepositoryTerminalServiceTests
{
    [Theory]
    [InlineData(nameof(RepositoryTerminalPlatform.MacOs), "open")]
    [InlineData(nameof(RepositoryTerminalPlatform.Linux), "x-terminal-emulator")]
    public void CreateTerminalStartInfo_UsesPlatformTerminal(
        string platformName,
        string expectedFileName)
    {
        const string repositoryPath = "/repos/project";
        var platform = Enum.Parse<RepositoryTerminalPlatform>(platformName);

        var startInfo =
            RepositoryTerminalService.CreateTerminalStartInfo(repositoryPath, platform);

        Assert.Equal(expectedFileName, startInfo.FileName);
        Assert.Equal(repositoryPath, startInfo.WorkingDirectory);
        Assert.False(startInfo.UseShellExecute);
        Assert.False(startInfo.CreateNoWindow);
    }

    [Fact]
    public void CreateTerminalStartInfo_UsesCmdKeepOpenOnWindows()
    {
        const string repositoryPath = @"C:\repos\project";

        var startInfo = RepositoryTerminalService.CreateTerminalStartInfo(
            repositoryPath,
            RepositoryTerminalPlatform.Windows);

        Assert.Equal("wt.exe", startInfo.FileName);
        Assert.False(startInfo.UseShellExecute);
        Assert.False(startInfo.CreateNoWindow);
        Assert.Equal(repositoryPath, startInfo.WorkingDirectory);
        Assert.Equal(
            new[] { "--title", "LovelyGit Terminal", "-d", repositoryPath },
            startInfo.ArgumentList);
    }

    [Fact]
    public void CreateTerminalStartInfo_OpensTerminalAppOnMacOs()
    {
        const string repositoryPath = "/repos/project";

        var startInfo = RepositoryTerminalService.CreateTerminalStartInfo(
            repositoryPath,
            RepositoryTerminalPlatform.MacOs);

        Assert.Equal(new[] { "-a", "Terminal", repositoryPath }, startInfo.ArgumentList);
    }

    [Fact]
    public void CreateTerminalStartInfo_ReliesOnWorkingDirectoryOnLinux()
    {
        const string repositoryPath = "/repos/project";

        var startInfo = RepositoryTerminalService.CreateTerminalStartInfo(
            repositoryPath,
            RepositoryTerminalPlatform.Linux);

        Assert.Empty(startInfo.ArgumentList);
    }
}
