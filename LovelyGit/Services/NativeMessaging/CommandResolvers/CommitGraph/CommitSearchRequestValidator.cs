namespace ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.CommitGraph;

internal static class CommitSearchRequestValidator
{
    private const int MinimumTextLength = 2;

    public static string? Validate(SearchCommitsCommandArguments arguments)
    {
        var query = arguments.Query.Trim();
        var author = arguments.Author.Trim();
        var scope = arguments.Scope.Trim();
        if (query.Length is > 0 and < MinimumTextLength
            || author.Length is > 0 and < MinimumTextLength)
        {
            return "Text and author filters need at least two characters.";
        }

        if (query.Length == 0 && author.Length == 0 && scope.Length == 0
            && arguments.AfterUnixSeconds == null && arguments.BeforeUnixSeconds == null)
        {
            return "Enter search text or choose at least one filter.";
        }

        if (arguments.AfterUnixSeconds is < 0 || arguments.BeforeUnixSeconds is < 0
            || arguments.AfterUnixSeconds >= arguments.BeforeUnixSeconds)
        {
            return "The commit date range is invalid.";
        }

        return null;
    }
}
