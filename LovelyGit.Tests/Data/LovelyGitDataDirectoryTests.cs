using ExpressThat.LovelyGit.Services.Data;

namespace LovelyGit.Tests.Data;

public sealed class LovelyGitDataDirectoryTests
{
    [Fact]
    public void Resolve_UsesExplicitOverrideAsAbsolutePath()
    {
        var relative = Path.Combine("artifacts", "isolated-data");

        var result = LovelyGitDataDirectory.Resolve(relative);

        Assert.Equal(Path.GetFullPath(relative), result);
    }

    [Fact]
    public void GetFilePath_CreatesOverriddenDirectory()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"lovelygit-data-{Guid.NewGuid():N}");
        try
        {
            var result = LovelyGitDataDirectory.GetFilePath("test.blite", directory);

            Assert.True(Directory.Exists(directory));
            Assert.Equal(Path.Combine(directory, "test.blite"), result);
        }
        finally
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, recursive: true);
            }
        }
    }
}
