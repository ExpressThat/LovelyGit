using ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.CommitGraph;

namespace LovelyGit.Tests.Git.CommitSearch;

public sealed class CommitSearchRequestValidatorTests
{
    [Theory]
    [MemberData(nameof(InvalidRequests))]
    public void Validate_RejectsInvalidBoundaries(
        SearchCommitsCommandArguments request,
        string expected)
    {
        Assert.Equal(expected, CommitSearchRequestValidator.Validate(request));
    }

    [Theory]
    [MemberData(nameof(ValidRequests))]
    public void Validate_AcceptsTextAndFilterOnlySearches(SearchCommitsCommandArguments request)
    {
        Assert.Null(CommitSearchRequestValidator.Validate(request));
    }

    public static TheoryData<SearchCommitsCommandArguments, string> InvalidRequests() => new()
    {
        { new(), "Enter search text or choose at least one filter." },
        { new() { Query = "x" }, "Text and author filters need at least two characters." },
        { new() { Author = "x" }, "Text and author filters need at least two characters." },
        { new() { AfterUnixSeconds = -1 }, "The commit date range is invalid." },
        {
            new() { AfterUnixSeconds = 20, BeforeUnixSeconds = 10 },
            "The commit date range is invalid."
        },
    };

    public static TheoryData<SearchCommitsCommandArguments> ValidRequests() => new()
    {
        new() { Query = "fix" },
        new() { Author = "Al" },
        new() { AfterUnixSeconds = 10 },
        new() { BeforeUnixSeconds = 20 },
        new() { AfterUnixSeconds = 10, BeforeUnixSeconds = 20 },
    };
}
