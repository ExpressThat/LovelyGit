using ColorCode;
using ColorCode.Common;
using ColorCode.Compilation;
using ColorCode.Parsing;
using DiffPlex;
using DiffPlex.DiffBuilder;
using DiffPlex.DiffBuilder.Model;
using ExpressThat.LovelyGit.Services.Data.Repositorys;
using ExpressThat.LovelyGit.Services.Git.CommitGraph.Details;
using ExpressThat.LovelyGit.Services.Git.CommitGraph.Models;
using ExpressThat.LovelyGit.Services.Git.Diffing;
using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser;

namespace ExpressThat.LovelyGit.Services.Git.CommitGraph;

internal sealed partial class CommitFileDiffService : IDisposable
{
    private static async Task<CommitFileDiffResponse> BuildCommitFileDiffAsync(
        string repositoryPath,
        string commitHash,
        int parentIndex,
        string path,
        CommitDiffViewMode viewMode,
        bool ignoreWhitespace,
        CancellationToken cancellationToken)
    {
        var source = await BuildCommitFileDiffSourceAsync(
                repositoryPath,
                commitHash,
                parentIndex,
                path,
                cancellationToken)
            .ConfigureAwait(false);
        return BuildResponseFromSource(commitHash, path, viewMode, ignoreWhitespace, source);
    }

    private async Task<CommitFileDiffResponse?> TryGetCachedDiffAsync(
        Guid repositoryId,
        string commitHash,
        string path,
        CommitDiffViewMode viewMode,
        bool ignoreWhitespace,
        CancellationToken cancellationToken)
    {
        try
        {
            return await _commitGraphRepository
                .GetCommitFileDiffAsync(
                    repositoryId,
                    commitHash,
                    path,
                    viewMode,
                    ignoreWhitespace,
                    cancellationToken)
                .ConfigureAwait(false);
        }
        catch when (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await _commitGraphRepository
                    .ClearCommitFileDiffsAsync(repositoryId, commitHash, CancellationToken.None)
                    .ConfigureAwait(false);
            }
            catch
            {
            }

            return null;
        }
    }

    private static async Task<CommitFileDiffSource> BuildCommitFileDiffSourceAsync(
        string repositoryPath,
        string commitHash,
        int parentIndex,
        string path,
        CancellationToken cancellationToken)
    {
        if (!GitObjectId.TryParse(commitHash, out var commitId))
        {
            throw new InvalidDataException("CommitHash is invalid.");
        }

        using var repository = await LovelyGitRepository.OpenAsync(repositoryPath, cancellationToken)
            .ConfigureAwait(false);
        var commit = await repository.GetCommitAsync(commitId, cancellationToken).ConfigureAwait(false);
        GitCommit? comparisonParent = null;
        if (commit.ParentHashes.Count > 0 && parentIndex < commit.ParentHashes.Count)
        {
            comparisonParent = await repository.GetCommitAsync(commit.ParentHashes[parentIndex], cancellationToken)
                .ConfigureAwait(false);
        }
        else if (parentIndex != 0)
        {
            throw new ArgumentOutOfRangeException(nameof(parentIndex), "Commit parent does not exist.");
        }

        var comparison = await repository
            .GetChangedTreeFilesAsync(comparisonParent?.TreeHash, commit.TreeHash, cancellationToken)
            .ConfigureAwait(false);
        comparison.ParentFiles.TryGetValue(path, out var oldFile);
        comparison.CurrentFiles.TryGetValue(path, out var newFile);
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

    private static CommitFileDiffResponse BuildResponseFromSource(
        string commitHash,
        string path,
        CommitDiffViewMode viewMode,
        bool ignoreWhitespace,
        CommitFileDiffSource source)
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

        if (DiffInputGuard.ShouldUseFastDiff(source.OldText, source.NewText))
        {
            return CompactDiffPayloadBuilder.CompactIfUseful(FastLineDiffBuilder.Build(
                commitHash,
                path,
                source.Status,
                viewMode,
                ignoreWhitespace,
                source.OldText,
                source.NewText));
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
        var model = new SideBySideDiffBuilder(new Differ()).BuildDiffModel(oldText, newText, ignoreWhitespace);
        var syntaxSpanBuilder = SyntaxSpanBuilder.Create(
            language,
            oldText.Length + newText.Length,
            MaxSyntaxHighlightedCharacters,
            MaxSyntaxHighlightedLineLength);
        var lineCount = Math.Max(model.OldText.Lines.Count, model.NewText.Lines.Count);
        var lines = new List<CommitFileDiffLine>(lineCount);
        for (var index = 0; index < lineCount; index++)
        {
            var oldLine = index < model.OldText.Lines.Count ? model.OldText.Lines[index] : null;
            var newLine = index < model.NewText.Lines.Count ? model.NewText.Lines[index] : null;
            var oldLineText = oldLine?.Text ?? string.Empty;
            var newLineText = newLine?.Text ?? string.Empty;

            lines.Add(new CommitFileDiffLine
            {
                OldLineNumber = oldLine?.Position,
                NewLineNumber = newLine?.Position,
                OldText = oldLineText,
                NewText = newLineText,
                ChangeType = GetSideBySideChangeType(oldLine, newLine),
                OldSyntaxSpans = BuildSyntaxSpans(oldLineText, syntaxSpanBuilder),
                NewSyntaxSpans = BuildSyntaxSpans(newLineText, syntaxSpanBuilder),
                OldChangeSpans = BuildChangeSpans(oldLine),
                NewChangeSpans = BuildChangeSpans(newLine),
            });
        }

        return new CommitFileDiffResponse
        {
            CommitHash = commitHash,
            Path = path,
            Status = status,
            ViewMode = CommitDiffViewMode.SideBySide,
            IsBinary = false,
            HasDifferences = model.OldText.HasDifferences || model.NewText.HasDifferences,
            Lines = lines,
        };
    }

}
