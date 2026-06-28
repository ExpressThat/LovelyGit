using ExpressThat.LovelyGit.Services.Platform;

namespace LovelyGit.Tests.Platform;

public sealed class RepositoryRevealServiceTests
{
    [Theory]
    [InlineData(nameof(RepositoryRevealPlatform.Windows), "explorer.exe")]
    [InlineData(nameof(RepositoryRevealPlatform.MacOs), "open")]
    [InlineData(nameof(RepositoryRevealPlatform.Linux), "xdg-open")]
    public void CreateRevealStartInfo_UsesPlatformFileManager(
        string platformName,
        string expectedFileName)
    {
        const string repositoryPath = "/repos/project";
        var platform = Enum.Parse<RepositoryRevealPlatform>(platformName);

        var startInfo =
            RepositoryRevealService.CreateRevealStartInfo(repositoryPath, platform);

        Assert.Equal(expectedFileName, startInfo.FileName);
        Assert.False(startInfo.UseShellExecute);
        Assert.True(startInfo.CreateNoWindow);
        Assert.Equal(new[] { repositoryPath }, startInfo.ArgumentList);
    }
}
