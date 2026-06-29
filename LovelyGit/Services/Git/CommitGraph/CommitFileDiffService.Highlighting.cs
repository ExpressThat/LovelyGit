using ColorCode;
using ColorCode.Common;
using ColorCode.Compilation;
using ColorCode.Parsing;
using ExpressThat.LovelyGit.Services.Git.CommitGraph.Models;

namespace ExpressThat.LovelyGit.Services.Git.CommitGraph;

internal sealed partial class CommitFileDiffService
{
    private static List<CommitFileDiffSyntaxSpan> BuildSyntaxSpans(
        string text,
        ILanguage? language)
    {
        if (language == null
            || string.IsNullOrEmpty(text)
            || text.Length > MaxSyntaxHighlightedLineLength)
        {
            return new List<CommitFileDiffSyntaxSpan>();
        }

        var spans = new List<CommitFileDiffSyntaxSpan>();
        var cursor = 0;
        var repository = new LanguageRepository(
            new Dictionary<string, ILanguage>(StringComparer.OrdinalIgnoreCase));
        var compiler = new LanguageCompiler(
            new Dictionary<string, CompiledLanguage>(StringComparer.OrdinalIgnoreCase),
            new ReaderWriterLockSlim());
        var parser = new LanguageParser(compiler, repository);
        try
        {
            parser.Parse(text, language, (chunk, scopes) =>
            {
                var chunkStart = text.IndexOf(chunk, cursor, StringComparison.Ordinal);
                if (chunkStart < 0)
                {
                    chunkStart = cursor;
                }

                foreach (var scope in scopes)
                {
                    spans.Add(new CommitFileDiffSyntaxSpan
                    {
                        Start = chunkStart + scope.Index,
                        Length = scope.Length,
                        Scope = scope.Name,
                    });
                }

                cursor = Math.Min(text.Length, chunkStart + chunk.Length);
            });
        }
        catch
        {
            return new List<CommitFileDiffSyntaxSpan>();
        }

        return spans;
    }
}
