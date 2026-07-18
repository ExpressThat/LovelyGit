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

    private static int Initialize(DirectoryInfo directory, int[] count)
    {
        File.WriteAllText(Path.Combine(directory.FullName, "fixture.txt"), "fixture");
        return ++count[0];
    }
}
