using ColorCode;
using ColorCode.Common;
using ColorCode.Compilation;
using ColorCode.Parsing;
using ExpressThat.LovelyGit.Services.Git.CommitGraph.Details;
using ExpressThat.LovelyGit.Services.Git.CommitGraph.Models;
using ExpressThat.LovelyGit.Services.Git.Diffing;
using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser;

namespace ExpressThat.LovelyGit.Services.Git.CommitGraph;

internal sealed partial class CommitFileDiffService : IDisposable
{
    private async Task<CommitFileDiffResponse> BuildCommitFileDiffAsync(
        string repositoryPath,
        string commitHash,
        string? comparisonCommitHash,
        int parentIndex,
        string path,
        CommitDiffViewMode viewMode,
        bool ignoreWhitespace,
        CancellationToken cancellationToken)
    {
        var key = string.Concat(repositoryPath, '\0', commitHash, '\0', comparisonCommitHash, '\0',
            parentIndex, '\0', path);
        if (!_sourceCache.TryGet(key, out var source))
        {
            source = await BuildCommitFileDiffSourceAsync(
                    repositoryPath,
                    commitHash,
                    comparisonCommitHash,
                    parentIndex,
                    path,
                    cancellationToken)
                .ConfigureAwait(false);
            _sourceCache.Set(key, source);
        }
        var hasCompressedSourceBundle = _sourceCache.TryGetCompressedSourceBundle(
            key,
            out var compressedSourceBundle);
        var response = BuildResponseFromSource(
            commitHash,
            path,
            viewMode,
            ignoreWhitespace,
            source,
            hasCompressedSourceBundle ? compressedSourceBundle : null);
        if (!string.IsNullOrEmpty(response.CompactSourceBundleGzipBase64)
            && !hasCompressedSourceBundle)
        {
            _sourceCache.SetCompressedSourceBundle(
                key,
                response.CompactSourceBundleGzipBase64);
        }
        return response;
    }

    private static async Task<CommitFileDiffSource> BuildCommitFileDiffSourceAsync(
        string repositoryPath,
        string commitHash,
        string? comparisonCommitHash,
        int parentIndex,
        string path,
        CancellationToken cancellationToken)
    {
        if (!GitObjectId.TryParse(commitHash, out var commitId))
        {
            throw new InvalidDataException("CommitHash is invalid.");
        }

        using var repository = await LovelyGitRepository
            .OpenObjectDatabaseAsync(repositoryPath, cancellationToken)
            .ConfigureAwait(false);
        var commit = await repository.GetCommitAsync(commitId, cancellationToken).ConfigureAwait(false);
        var comparisonParent = await ResolveComparisonCommitAsync(
                repository,
                commit,
                comparisonCommitHash,
                parentIndex,
                cancellationToken)
            .ConfigureAwait(false);

        var oldFile = comparisonParent?.TreeHash is { } parentTree
            ? await repository.TryGetTreeFileAsync(parentTree, path, cancellationToken)
                .ConfigureAwait(false)
            : null;
        var newFile = commit.TreeHash is { } currentTree
            ? await repository.TryGetTreeFileAsync(currentTree, path, cancellationToken)
                .ConfigureAwait(false)
            : null;
        if (oldFile == null && newFile == null)
        {
            throw new FileNotFoundException("Changed file not found in commit.", path);
        }

        var analyzer = new BlobLineAnalyzer(repository);
        var oldBlob = oldFile == null
            ? new BlobText(false, string.Empty)
            : await analyzer.ReadTextAsync(oldFile, cancellationToken).ConfigureAwait(false);
        var newBlob = newFile == null
            ? new BlobText(false, string.Empty)
            : await analyzer.ReadTextAsync(newFile, cancellationToken).ConfigureAwait(false);
        var isBinary = oldBlob.IsBinary || newBlob.IsBinary;
        var status = GetStatus(oldFile, newFile);

        if (isBinary)
        {
            return new CommitFileDiffSource
            {
                Status = status,
                IsBinary = true,
            };
        }

        var language = oldBlob.Text.Length + newBlob.Text.Length <= MaxSyntaxHighlightedCharacters
            ? ResolveLanguage(path)
            : null;

        return new CommitFileDiffSource
        {
            Status = status,
            IsBinary = false,
            OldText = oldBlob.Text,
            NewText = newBlob.Text,
            Language = language,
        };
    }

    internal static CommitFileDiffResponse BuildResponseFromSource(
        string commitHash,
        string path,
        CommitDiffViewMode viewMode,
        bool ignoreWhitespace,
        CommitFileDiffSource source,
        string? compressedSourceBundle = null)
    {
        if (source.IsBinary)
        {
            return new CommitFileDiffResponse
            {
                CommitHash = commitHash,
                Path = path,
                Status = source.Status,
                ViewMode = viewMode,
                IsBinary = true,
                HasDifferences = true,
            };
        }

        if (DiffInputGuard.ShouldUseVirtualText(source.OldText, source.NewText)
            || DiffInputGuard.ShouldUseFastDiff(source.OldText, source.NewText))
        {
            return CompactDiffPayloadBuilder.CompactIfUseful(LargeDiffPayloadBuilder.Build(
                commitHash,
                path,
                source.Status,
                viewMode,
                ignoreWhitespace,
                source.OldText,
                source.NewText,
                compressedSourceBundle));
        }

        var response = viewMode == CommitDiffViewMode.SideBySide
            ? BuildSideBySideResponse(
                commitHash,
                path,
                source.Status,
                source.OldText,
                source.NewText,
                source.Language,
                ignoreWhitespace)
            : BuildCombinedResponse(
                commitHash,
                path,
                source.Status,
                source.OldText,
                source.NewText,
                source.Language,
                ignoreWhitespace);
        return CompactDiffPayloadBuilder.CompactIfUseful(response);
    }

    private static CommitFileDiffResponse BuildSideBySideResponse(
        string commitHash,
        string path,
        string status,
        string oldText,
        string newText,
        ILanguage? language,
        bool ignoreWhitespace)
    {
        var model = LineDiffEngine.Build(oldText, newText, ignoreWhitespace);
        var syntaxSpanBuilder = SyntaxSpanBuilder.Create(
            language,
            oldText.Length + newText.Length,
            MaxSyntaxHighlightedCharacters,
            MaxSyntaxHighlightedLineLength);
        var lines = new List<CommitFileDiffLine>(model.Rows.Count);
        foreach (var row in model.Rows)
        {
            var oldLineText = row.OldIndex is int oldIndex ? model.OldLines[oldIndex] : string.Empty;
            var newLineText = row.NewIndex is int newIndex ? model.NewLines[newIndex] : string.Empty;
            var changeSpans = LineDiffRendering.ChangeSpans(oldLineText, newLineText, row);

            lines.Add(new CommitFileDiffLine
            {
                OldLineNumber = row.OldIndex + 1,
                NewLineNumber = row.NewIndex + 1,
                OldText = oldLineText,
                NewText = newLineText,
                ChangeType = LineDiffRendering.ChangeType(row),
                OldSyntaxSpans = BuildSyntaxSpans(oldLineText, syntaxSpanBuilder),
                NewSyntaxSpans = BuildSyntaxSpans(newLineText, syntaxSpanBuilder),
                OldChangeSpans = changeSpans.Old,
                NewChangeSpans = changeSpans.New,
            });
        }

        return new CommitFileDiffResponse
        {
            CommitHash = commitHash,
            Path = path,
            Status = status,
            ViewMode = CommitDiffViewMode.SideBySide,
            IsBinary = false,
            HasDifferences = model.HasDifferences,
            Lines = lines,
        };
    }

}
