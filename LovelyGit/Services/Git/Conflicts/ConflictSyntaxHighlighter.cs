using ColorCode;
using ColorCode.Common;
using ColorCode.Compilation;
using ColorCode.Parsing;
using ExpressThat.LovelyGit.Services.Git.CommitGraph.Models;

namespace ExpressThat.LovelyGit.Services.Git.Conflicts;

internal sealed class ConflictSyntaxHighlighter
{
    private const int MaxHighlightedFileCharacters = 120_000;
    private const int MaxHighlightedLineCharacters = 2_000;

    private readonly LanguageCompiler? _compiler;
    private readonly ILanguage? _language;
    private readonly LanguageRepository? _repository;

    private ConflictSyntaxHighlighter(
        ILanguage? language,
        LanguageCompiler? compiler,
        LanguageRepository? repository)
    {
        _compiler = compiler;
        _language = language;
        _repository = repository;
    }

    public static ConflictSyntaxHighlighter Create(ILanguage? language, int fileLength)
    {
        if (language == null || fileLength > MaxHighlightedFileCharacters)
        {
            return new ConflictSyntaxHighlighter(null, null, null);
        }

        var repository = new LanguageRepository(
            new Dictionary<string, ILanguage>(StringComparer.OrdinalIgnoreCase));
        var compiler = new LanguageCompiler(
            new Dictionary<string, CompiledLanguage>(StringComparer.OrdinalIgnoreCase),
            new ReaderWriterLockSlim());
        return new ConflictSyntaxHighlighter(language, compiler, repository);
    }

    public List<CommitFileDiffSyntaxSpan> BuildSpans(string text)
    {
        if (_language == null
            || _compiler == null
            || _repository == null
            || string.IsNullOrEmpty(text)
            || text.Length > MaxHighlightedLineCharacters)
        {
            return [];
        }

        var spans = new List<CommitFileDiffSyntaxSpan>();
        try
        {
            var parser = new LanguageParser(_compiler, _repository);
            var cursor = 0;
            parser.Parse(text, _language, (chunk, scopes) =>
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
            return [];
        }

        return spans;
    }
}
