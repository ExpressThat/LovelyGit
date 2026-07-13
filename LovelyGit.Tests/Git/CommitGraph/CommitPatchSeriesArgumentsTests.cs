using ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.CommitGraph;

namespace LovelyGit.Tests.Git.CommitGraph;

public sealed class CommitPatchSeriesArgumentsTests
{
    [Fact]
    public void TryParse_PreservesOrderAndRejectsDuplicates()
    {
        var first = new string('1', 40);
        var second = new string('2', 40);
        var arguments = new CommitPatchSeriesCommandArguments
        {
            RepositoryId = Guid.NewGuid(),
            CommitHashes = [first, second],
        };

        Assert.True(CommitPatchSeriesArguments.TryParse(arguments, out var ids, out _));
        Assert.Equal([first, second], ids.Select(id => id.ToString()));

        arguments.CommitHashes = [first, first];
        Assert.False(CommitPatchSeriesArguments.TryParse(arguments, out _, out var error));
        Assert.Contains("duplicates", error, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(51)]
    public void TryParse_RejectsUnsafeCounts(int count)
    {
        var arguments = new CommitPatchSeriesCommandArguments
        {
            RepositoryId = Guid.NewGuid(),
            CommitHashes = Enumerable.Range(0, count)
                .Select(index => index.ToString("x40"))
                .ToList(),
        };

        Assert.False(CommitPatchSeriesArguments.TryParse(arguments, out _, out var error));
        Assert.Contains("between 1 and 50", error, StringComparison.Ordinal);
    }
}
