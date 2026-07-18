using System.Diagnostics;
using System.Text;
using System.Text.Json;
using ExpressThat.LovelyGit.Services.Git.SparseCheckout;
using ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.SparseCheckout;
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
            Assert.Equal(100_000, state.PatternCount);
            Assert.StartsWith("/src/module-000000/**\n", state.PatternText);
            Assert.EndsWith("/src/module-099999/**", state.PatternText);
            Assert.True(elapsed < TimeSpan.FromMilliseconds(50), $"Read took {elapsed}.");
            Assert.True(allocated < 11_000_000, $"Read allocated {allocated:N0} bytes.");

            var serializeAllocatedBefore = GC.GetTotalAllocatedBytes(precise: true);
            var serializeStartedAt = Stopwatch.GetTimestamp();
            var compact = SparseCheckoutPayloadCompactor.CompactIfUseful(state);
            var json = JsonSerializer.Serialize(
                compact,
                SparseCheckoutJsonSerializerContext.Default.SparseCheckoutState);
            var serializeElapsed = Stopwatch.GetElapsedTime(serializeStartedAt);
            var serializeAllocated = GC.GetTotalAllocatedBytes(true) - serializeAllocatedBefore;
            output.WriteLine(
                $"SerializeMs={serializeElapsed.TotalMilliseconds:F2}; " +
                $"SerializeAllocatedBytes={serializeAllocated:N0}; PayloadChars={json.Length:N0}");
            Assert.Empty(compact.PatternText);
            Assert.True(json.Length < 500_000, $"Payload retained {json.Length:N0} characters.");
            Assert.True(serializeElapsed < TimeSpan.FromMilliseconds(50));
            Assert.True(serializeAllocated < 10_000_000);
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
