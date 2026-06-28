using ColorCode;
using ExpressThat.LovelyGit.Services.Git.Conflicts;

namespace LovelyGit.Tests.Git.Conflicts;

public sealed class ConflictSyntaxHighlighterTests
{
    [Fact]
    public void BuildSpans_DoesNotCarryStringScopeAcrossTypeScriptLine()
    {
        const string line = "describe(\"LovelyGit conflict fixture\", () => {";
        var highlighter = ConflictSyntaxHighlighter.Create(Languages.Typescript, line.Length);

        var spans = highlighter.BuildSpans(line);
        var describeStringSpan = spans.FirstOrDefault(span =>
            span.Scope == "String"
            && span.Start <= 0
            && span.Start + span.Length >= "describe".Length);

        Assert.Null(describeStringSpan);
    }
}
