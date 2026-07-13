using System.Text;
using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser;

namespace LovelyGit.Tests.Git.LovelyFastGitParser;

public sealed class GitObjectParsersSearchTests
{
    [Theory]
    [InlineData("allocation", true)]
    [InlineData("ALLOCATION", true)]
    [InlineData("author@example.test", true)]
    [InlineData("tail marker", true)]
    [InlineData("missing", false)]
    public void ParseCommitSearchHeader_MatchesAsciiWithoutChangingCaseSemantics(
        string query,
        bool expected)
    {
        var data = Encoding.UTF8.GetBytes(
            "tree 0123456789012345678901234567890123456789\n" +
            "author Test Author <author@example.test> 1700000000 +0000\n" +
            "committer Test Author <author@example.test> 1700000000 +0000\n\n" +
            "Improve Allocation behavior with a tail marker");
        var queryBytes = Encoding.UTF8.GetBytes(query);
        var id = GitObjectId.Parse("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa");

        var result = GitObjectParsers.ParseCommitSearchHeader(
            id,
            data,
            queryBytes,
            query,
            [],
            string.Empty);

        Assert.Equal(expected, result.TextMatches);
    }

    [Fact]
    public void ParseCommitSearchHeader_DoesNotMatchPastTheLastValidStart()
    {
        var data = Encoding.UTF8.GetBytes(
            "tree 0123456789012345678901234567890123456789\n" +
            "author User <user@example.test> 1700000000 +0000\n\nabcX");
        var query = Encoding.UTF8.GetBytes("xyz");
        var id = GitObjectId.Parse("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa");

        var result = GitObjectParsers.ParseCommitSearchHeader(
            id, data, query, "xyz", [], string.Empty);

        Assert.False(result.TextMatches);
    }
}
