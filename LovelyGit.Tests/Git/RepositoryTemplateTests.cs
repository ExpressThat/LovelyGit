namespace LovelyGit.Tests.Git;

public sealed class RepositoryTemplateTests
{
    [Fact]
    public void PreparedCopies_AreExclusiveAndInitializeOnlyOnce()
    {
        var initializationCount = new int[1];
        var template = new RepositoryTemplate<int>(
            "lovelygit-template-test-",
            directory => Initialize(directory, initializationCount));
        template.PrepareCopies(2);
        var (first, firstState) = template.CreateCopy("unused-first-");
        var (second, secondState) = template.CreateCopy("unused-second-");
        try
        {
            File.WriteAllText(Path.Combine(first.FullName, "fixture.txt"), "changed");

            Assert.Equal(1, initializationCount[0]);
            Assert.Equal(1, firstState);
            Assert.Equal(1, secondState);
            Assert.Equal("fixture", File.ReadAllText(Path.Combine(second.FullName, "fixture.txt")));
            Assert.NotEqual(first.FullName, second.FullName);
        }
        finally
        {
            first.Delete(recursive: true);
            second.Delete(recursive: true);
        }
    }

    [Fact]
    public void LifetimeCleanup_RemovesReadOnlyGitStyleFiles()
    {
        var directory = Directory.CreateTempSubdirectory("lovelygit-template-cleanup-");
        var file = new FileInfo(Path.Combine(directory.FullName, "object"));
        File.WriteAllText(file.FullName, "immutable");
        file.IsReadOnly = true;

        RepositoryTemplateLifetime.DeleteDirectory(directory);

        Assert.False(directory.Exists);
    }

    [Fact]
    public void LifetimeCleanup_LeavesAnActivelyOwnedRootUntouched()
    {
        var directory = Directory.CreateTempSubdirectory("lovelygit-owned-root-");
        var ownerPath = Path.Combine(directory.FullName, ".owner");
        using (var process = System.Diagnostics.Process.GetCurrentProcess())
        {
            File.WriteAllText(
                ownerPath,
                $"{process.Id}|{process.StartTime.ToUniversalTime().Ticks}");
            Assert.False(RepositoryTemplateLifetime.TryDeleteRoot(directory, TimeSpan.Zero));
            Assert.True(directory.Exists);
        }

        File.WriteAllText(ownerPath, $"{int.MaxValue}|0");
        Assert.True(RepositoryTemplateLifetime.TryDeleteRoot(directory, TimeSpan.Zero));
        Assert.False(directory.Exists);
    }

    [Fact]
    public void LifetimeCleanup_LeavesMalformedOwnershipForSafeInspection()
    {
        var directory = Directory.CreateTempSubdirectory("lovelygit-malformed-owner-");
        var ownerPath = Path.Combine(directory.FullName, ".owner");
        File.WriteAllText(ownerPath, "not-an-owner");

        Assert.False(RepositoryTemplateLifetime.TryDeleteRoot(directory, TimeSpan.Zero));
        Assert.True(directory.Exists);

        File.Delete(ownerPath);
        RepositoryTemplateLifetime.DeleteDirectory(directory);
    }

    private static int Initialize(DirectoryInfo directory, int[] count)
    {
        File.WriteAllText(Path.Combine(directory.FullName, "fixture.txt"), "fixture");
        return ++count[0];
    }
}
