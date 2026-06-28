using ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.WorkingTree;

namespace LovelyGit.Tests.Git.WorkingTree;

public sealed class RevealWorkingTreeFileCommandResolverTests
{
    [Fact]
    public void ResolveWorkingTreePath_AllowsRelativePathInsideRepository()
    {
        var root = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "lovelygit-root"));

        var path = RevealWorkingTreeFileCommandResolver.ResolveWorkingTreePath(
            root,
            Path.Combine("src", "file.txt"));

        Assert.Equal(Path.Combine(root, "src", "file.txt"), path);
    }

    [Fact]
    public void ResolveWorkingTreePath_RejectsEscapingPath()
    {
        var root = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "lovelygit-root"));

        var exception = Assert.Throws<InvalidOperationException>(() =>
            RevealWorkingTreeFileCommandResolver.ResolveWorkingTreePath(root, "../escape.txt"));

        Assert.Equal("Path must be inside the repository.", exception.Message);
    }
}
