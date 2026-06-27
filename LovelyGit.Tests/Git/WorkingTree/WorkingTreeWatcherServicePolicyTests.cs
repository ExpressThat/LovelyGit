using ExpressThat.LovelyGit.Services.Git.WorkingTree;

namespace LovelyGit.Tests.Git.WorkingTree;

public sealed class WorkingTreeWatcherServicePolicyTests
{
    [Fact]
    public void ShouldWatchWorkTreeRecursively_ReturnsFalseForMissingDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));

        Assert.False(WorkingTreeWatcherService.ShouldWatchWorkTreeRecursively(path));
    }

    [Fact]
    public void ShouldWatchWorkTreeRecursively_ReturnsTrueForSmallDirectory()
    {
        using var directory = TemporaryDirectory.Create("lovelygit-watch-small-");
        Directory.CreateDirectory(Path.Combine(directory.Path, "src"));
        Directory.CreateDirectory(Path.Combine(directory.Path, "tests"));

        Assert.True(WorkingTreeWatcherService.ShouldWatchWorkTreeRecursively(directory.Path));
    }

    [Fact]
    public void ShouldWatchWorkTreeRecursively_ReturnsFalseForLargeDirectory()
    {
        using var directory = TemporaryDirectory.Create("lovelygit-watch-large-");
        for (var index = 0; index < 2001; index++)
        {
            Directory.CreateDirectory(Path.Combine(directory.Path, $"d{index}"));
        }

        Assert.False(WorkingTreeWatcherService.ShouldWatchWorkTreeRecursively(directory.Path));
    }

    private sealed class TemporaryDirectory : IDisposable
    {
        private readonly DirectoryInfo _directory;

        private TemporaryDirectory(DirectoryInfo directory)
        {
            _directory = directory;
            Path = directory.FullName;
        }

        public string Path { get; }

        public static TemporaryDirectory Create(string prefix)
        {
            return new TemporaryDirectory(Directory.CreateTempSubdirectory(prefix));
        }

        public void Dispose()
        {
            _directory.Delete(recursive: true);
        }
    }
}
