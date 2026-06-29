using ExpressThat.LovelyGit.Services.Git.CommitGraph.Models;
using ExpressThat.LovelyGit.Services.Git.Diffing;

namespace ExpressThat.LovelyGit.Services.Git.CommitGraph;

internal sealed partial class CommitFileDiffService
{
    private static List<CommitFileDiffSyntaxSpan> BuildSyntaxSpans(
        string text,
        SyntaxSpanBuilder syntaxSpanBuilder)
    {
        return syntaxSpanBuilder.BuildSpans(text);
    }
}
