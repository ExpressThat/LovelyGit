using System.Diagnostics;
using ExpressThat.LovelyGit.Services.Git.Branches;
using ExpressThat.LovelyGit.Services.Git.Cli;
using ExpressThat.LovelyGit.Services.Git.Tags;
using Xunit.Abstractions;

namespace LovelyGit.Tests.Git.Tags;

[Collection(PerformanceTestCollection.Name)]
public sealed class GitCommitExistencePerformanceTests(ITestOutputHelper output)
{
    private const int RefCountPerKind = 500;
    private static readonly RepositoryTemplate<string> Template = new(
        "lovelygit-tag-validation-template-",
        InitializeTemplate, prewarmCopies: 2);

    [Fact]
    public async Task ExistingCommitValidation_DoesNotScaleWithRefCount()
    {
        var (directory, head) = Template.CreateCopy("lovelygit-tag-validation-");
        try
        {
            await GitCommitExistenceReader.EnsureExistsAsync(
                directory.FullName, head, CancellationToken.None);
            GC.Collect();
            var allocatedBefore = GC.GetTotalAllocatedBytes(precise: true);
            var startedAt = Stopwatch.GetTimestamp();
            for (var iteration = 0; iteration < 10; iteration++)
            {
                await GitCommitExistenceReader.EnsureExistsAsync(
                    directory.FullName, head, CancellationToken.None);
            }

            var elapsed = Stopwatch.GetElapsedTime(startedAt);
            var allocated = GC.GetTotalAllocatedBytes(true) - allocatedBefore;
            output.WriteLine($"ElapsedMs={elapsed.TotalMilliseconds:F2}");
            output.WriteLine($"AllocatedBytes={allocated:N0}");
            Assert.True(elapsed < TimeSpan.FromMilliseconds(100), $"Validations took {elapsed}.");
            Assert.True(allocated < 1_000_000, $"Validations allocated {allocated:N0} bytes.");
        }
        finally
        {
            DeleteDirectory(directory);
        }
    }

    [Fact]
    public async Task DetachedCheckout_DoesNotScaleWithRefCount()
    {
        var (directory, head) = Template.CreateCopy("lovelygit-checkout-validation-");
        try
        {
            var git = new GitCliService();
            var service = new GitBranchCommandService(git);
            var allocatedBefore = GC.GetTotalAllocatedBytes(precise: true);
            var startedAt = Stopwatch.GetTimestamp();

            await service.CheckoutCommitAsync(directory.FullName, head, CancellationToken.None);

            var elapsed = Stopwatch.GetElapsedTime(startedAt);
            var allocated = GC.GetTotalAllocatedBytes(true) - allocatedBefore;
            output.WriteLine($"CheckoutElapsedMs={elapsed.TotalMilliseconds:F2}");
            output.WriteLine($"CheckoutAllocatedBytes={allocated:N0}");
            var branch = await git.ExecuteBufferedAsync(
                ["branch", "--show-current"], directory.FullName);
            Assert.Empty(branch.StandardOutput.Trim());
            Assert.True(elapsed < TimeSpan.FromMilliseconds(150), $"Checkout took {elapsed}.");
            Assert.True(allocated < 1_000_000, $"Checkout allocated {allocated:N0} bytes.");
        }
        finally
        {
            DeleteDirectory(directory);
        }
    }

    private static string InitializeTemplate(DirectoryInfo directory)
    {
        var head = InitializedRepositoryTemplate.CopyInto(directory);
        PackedRefFixture.AddBranchRemoteTagSets(directory.FullName, head, RefCountPerKind);

        return head;
    }

    private static void DeleteDirectory(DirectoryInfo directory)
    {
        foreach (var file in directory.EnumerateFiles("*", SearchOption.AllDirectories))
        {
            file.Attributes = FileAttributes.Normal;
        }

        directory.Delete(true);
    }
}
