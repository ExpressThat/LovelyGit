using ColorCode;
using ColorCode.Common;
using ColorCode.Compilation;
using ColorCode.Parsing;
using ExpressThat.LovelyGit.Services.Git.CommitGraph.Models;

namespace ExpressThat.LovelyGit.Services.Git.Diffing;

internal sealed class SyntaxSpanBuilder
{
    private readonly LanguageCompiler? _compiler;
    private readonly ILanguage? _language;
    private readonly int _maxLineCharacters;
    private readonly LanguageRepository? _repository;

    private SyntaxSpanBuilder(
        ILanguage? language,
        int maxLineCharacters,
        LanguageCompiler? compiler,
        LanguageRepository? repository)
    {
        _compiler = compiler;
        _language = language;
        _maxLineCharacters = maxLineCharacters;
        _repository = repository;
    }

    public static SyntaxSpanBuilder Create(
        ILanguage? language,
        int fileLength,
        int maxFileCharacters,
        int maxLineCharacters)
    {
        if (language == null || fileLength > maxFileCharacters)
        {
            return new SyntaxSpanBuilder(null, maxLineCharacters, null, null);
        }

        var repository = new LanguageRepository(
            new Dictionary<string, ILanguage>(StringComparer.OrdinalIgnoreCase));
        var compiler = new LanguageCompiler(
            new Dictionary<string, CompiledLanguage>(StringComparer.OrdinalIgnoreCase),
            new ReaderWriterLockSlim());
        return new SyntaxSpanBuilder(language, maxLineCharacters, compiler, repository);
    }

    public List<CommitFileDiffSyntaxSpan> BuildSpans(string text)
    {
        if (_language == null
            || _compiler == null
            || _repository == null
            || string.IsNullOrEmpty(text)
            || text.Length > _maxLineCharacters)
        {
            return [];
        }

        var spans = new List<CommitFileDiffSyntaxSpan>();
        try
        {
            var cursor = 0;
            var parser = new LanguageParser(_compiler, _repository);
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
