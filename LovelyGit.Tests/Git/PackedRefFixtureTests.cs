using ExpressThat.LovelyGit.Services.Git.Cli;
using LovelyGit.Tests.Git.Branches;

namespace LovelyGit.Tests.Git;

public sealed class PackedRefFixtureTests
{
    [Fact]
    public async Task MixedRefsRemainVisibleToGitAndPreserveExistingHead()
    {
        var directory = Directory.CreateTempSubdirectory("lovelygit-packed-ref-fixture-");
        try
        {
            var head = InitializedRepositoryTemplate.CopyInto(directory, "master");
            var git = new GitCliService();

            PackedRefFixture.AddBranches(directory.FullName, head, 20);
            PackedRefFixture.AddBranchRemoteTagSets(directory.FullName, head, 10);

            var refs = (await git.ExecuteBufferedAsync(
                ["show-ref"], directory.FullName)).StandardOutput;
            Assert.Contains($"{head} refs/heads/master", refs);
            Assert.Contains($"{head} refs/heads/perf/branch-0000", refs);
            Assert.Contains($"{head} refs/heads/perf/branch-0019", refs);
            Assert.Contains($"{head} refs/remotes/origin/branch-9", refs);
            Assert.Contains($"{head} refs/tags/tag-9", refs);
            Assert.Equal(51, refs.Split('\n', StringSplitOptions.RemoveEmptyEntries).Length);
        }
        finally
        {
            TemporaryGitDirectory.Delete(directory);
        }
    }
}
