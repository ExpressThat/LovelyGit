using ColorCode;
using ExpressThat.LovelyGit.Services.Git.CommitGraph.Models;
using ExpressThat.LovelyGit.Services.Git.Diffing;

namespace ExpressThat.LovelyGit.Services.Git.Conflicts;

internal sealed class ConflictSyntaxHighlighter
{
    private const int MaxHighlightedFileCharacters = 120_000;
    private const int MaxHighlightedLineCharacters = 2_000;

    private readonly SyntaxSpanBuilder _builder;

    private ConflictSyntaxHighlighter(SyntaxSpanBuilder builder)
    {
        _builder = builder;
    }

    public static ConflictSyntaxHighlighter Create(ILanguage? language, int fileLength)
    {
        return new ConflictSyntaxHighlighter(SyntaxSpanBuilder.Create(
            language,
            fileLength,
            MaxHighlightedFileCharacters,
            MaxHighlightedLineCharacters));
    }

    public List<CommitFileDiffSyntaxSpan> BuildSpans(string text)
    {
        return _builder.BuildSpans(text);
    }
}
