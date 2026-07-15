using System.Diagnostics;
using System.Text;
using ExpressThat.LovelyGit.Services.Git.SparseCheckout;
using Xunit.Abstractions;

namespace LovelyGit.Tests.Git.SparseCheckout;

[Collection(PerformanceTestCollection.Name)]
public sealed class NativeSparseCheckoutReaderPerformanceTests(ITestOutputHelper output)
{
    [Fact]
    public async Task ReadAsync_LoadsLargePatternListWithinManagerBudget()
    {
        var directory = Directory.CreateTempSubdirectory("lovelygit-sparse-performance-");
        try
        {
            InitializedRepositoryTemplate.CopyInto(directory, "master");
            await EnableSparseCheckoutAsync(directory.FullName, 100_000);
            var reader = new NativeSparseCheckoutReader();
            var allocatedBefore = GC.GetTotalAllocatedBytes(precise: true);
            var startedAt = Stopwatch.GetTimestamp();

            var state = await reader.ReadAsync(directory.FullName, CancellationToken.None);

            var elapsed = Stopwatch.GetElapsedTime(startedAt);
            var allocated = GC.GetTotalAllocatedBytes(true) - allocatedBefore;
            output.WriteLine($"ElapsedMs={elapsed.TotalMilliseconds:F2}; AllocatedBytes={allocated:N0}");
            Assert.True(state.Enabled);
            Assert.False(state.ConeMode);
            Assert.Equal(100_000, state.Patterns.Count);
            Assert.True(elapsed < TimeSpan.FromMilliseconds(50), $"Read took {elapsed}.");
            Assert.True(allocated < 17_000_000, $"Read allocated {allocated:N0} bytes.");
        }
        finally
        {
            foreach (var file in directory.EnumerateFiles("*", SearchOption.AllDirectories))
            {
                file.Attributes = FileAttributes.Normal;
            }
            directory.Delete(recursive: true);
        }
    }

    private static async Task EnableSparseCheckoutAsync(string repositoryPath, int count)
    {
        var gitDirectory = Path.Combine(repositoryPath, ".git");
        await File.AppendAllTextAsync(
            Path.Combine(gitDirectory, "config"),
            "\n[core]\n\tsparseCheckout = true\n\tsparseCheckoutCone = false\n");
        var infoDirectory = Directory.CreateDirectory(Path.Combine(gitDirectory, "info"));
        await using var stream = new FileStream(
            Path.Combine(infoDirectory.FullName, "sparse-checkout"), FileMode.Create, FileAccess.Write);
        await using var writer = new StreamWriter(stream, Encoding.UTF8);
        for (var index = 0; index < count; index++)
        {
            await writer.WriteLineAsync($"/src/module-{index:D6}/**");
        }
    }
}
