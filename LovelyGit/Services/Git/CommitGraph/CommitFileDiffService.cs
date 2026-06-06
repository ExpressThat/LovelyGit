using ColorCode;
using ColorCode.Common;
using ColorCode.Compilation;
using ColorCode.Parsing;
using DiffPlex.DiffBuilder;
using DiffPlex.DiffBuilder.Model;
using ExpressThat.LovelyGit.Services.Data.Repositorys;
using ExpressThat.LovelyGit.Services.Git.CommitGraph.Details;
using ExpressThat.LovelyGit.Services.Git.CommitGraph.Models;
using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser;

namespace ExpressThat.LovelyGit.Services.Git.CommitGraph;

internal sealed class CommitFileDiffService : IDisposable
{
    private readonly CommitGraphRepository _commitGraphRepository;
    private readonly object _preparationLock = new();
    private readonly Dictionary<Guid, ActivePreparation> _activePreparations = new();
    private bool _disposed;

    public CommitFileDiffService(CommitGraphRepository commitGraphRepository)
    {
        _commitGraphRepository = commitGraphRepository;
    }

    public void StartPreparingCommitDiffs(
        Guid repositoryId,
        string repositoryPath,
        string commitHash,
        IReadOnlyList<CommitChangedFile> changedFiles)
    {
        ActivePreparation? previous = null;
        var shouldStart = true;
        lock (_preparationLock)
        {
            ThrowIfDisposed();
            if (_activePreparations.TryGetValue(repositoryId, out previous))
            {
                if (string.Equals(previous.CommitHash, commitHash, StringComparison.Ordinal))
                {
                    shouldStart = false;
                }
                else
                {
                    previous.CancellationTokenSource.Cancel();
                }
            }

            if (shouldStart)
            {
                var cancellationTokenSource = new CancellationTokenSource();
                var task = Task.Run(
                    () => PrepareCommitDiffsAsync(
                        repositoryId,
                        repositoryPath,
                        commitHash,
                        changedFiles,
                        previous?.CommitHash,
                        cancellationTokenSource.Token),
                    cancellationTokenSource.Token);

                _activePreparations[repositoryId] = new ActivePreparation(
                    commitHash,
                    cancellationTokenSource,
                    task);
            }
        }

        if (!shouldStart)
        {
            return;
        }

        previous?.Dispose();
    }

    public async Task<CommitFileDiffResponse> GetCommitFileDiffAsync(
        Guid repositoryId,
        string repositoryPath,
        string commitHash,
        string path,
        CommitDiffViewMode viewMode,
        CancellationToken cancellationToken)
    {
        var cached = await _commitGraphRepository
            .GetCommitFileDiffAsync(repositoryId, commitHash, path, viewMode, cancellationToken)
            .ConfigureAwait(false);
        if (cached != null)
        {
            return cached;
        }

        var response = await BuildCommitFileDiffAsync(
                repositoryPath,
                commitHash,
                path,
                viewMode,
                cancellationToken)
            .ConfigureAwait(false);

        await _commitGraphRepository
            .SaveCommitFileDiffAsync(repositoryId, commitHash, path, response, cancellationToken)
            .ConfigureAwait(false);

        return await _commitGraphRepository
            .GetCommitFileDiffAsync(repositoryId, commitHash, path, viewMode, cancellationToken)
            .ConfigureAwait(false) ?? response;
    }

    public void Dispose()
    {
        List<ActivePreparation> active;
        lock (_preparationLock)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            active = _activePreparations.Values.ToList();
            _activePreparations.Clear();
        }

        foreach (var preparation in active)
        {
            preparation.CancellationTokenSource.Cancel();
            preparation.Dispose();
        }
    }

    private async Task PrepareCommitDiffsAsync(
        Guid repositoryId,
        string repositoryPath,
        string commitHash,
        IReadOnlyList<CommitChangedFile> changedFiles,
        string? previousCommitHash,
        CancellationToken cancellationToken)
    {
        try
        {
            if (!string.IsNullOrEmpty(previousCommitHash))
            {
                await _commitGraphRepository
                    .ClearCommitFileDiffsAsync(repositoryId, previousCommitHash, CancellationToken.None)
                    .ConfigureAwait(false);
            }

            foreach (var file in changedFiles)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (file.IsBinary)
                {
                    await SaveIfMissingAsync(
                            repositoryId,
                            repositoryPath,
                            commitHash,
                            file.Path,
                            CommitDiffViewMode.SideBySide,
                            cancellationToken)
                        .ConfigureAwait(false);
                    await SaveIfMissingAsync(
                            repositoryId,
                            repositoryPath,
                            commitHash,
                            file.Path,
                            CommitDiffViewMode.Combined,
                            cancellationToken)
                        .ConfigureAwait(false);
                    continue;
                }

                await SaveIfMissingAsync(
                        repositoryId,
                        repositoryPath,
                        commitHash,
                        file.Path,
                        CommitDiffViewMode.SideBySide,
                        cancellationToken)
                    .ConfigureAwait(false);
                await SaveIfMissingAsync(
                        repositoryId,
                        repositoryPath,
                        commitHash,
                        file.Path,
                        CommitDiffViewMode.Combined,
                        cancellationToken)
                    .ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch
        {
        }
        finally
        {
            lock (_preparationLock)
            {
                if (_activePreparations.TryGetValue(repositoryId, out var active)
                    && string.Equals(active.CommitHash, commitHash, StringComparison.Ordinal))
                {
                    _activePreparations.Remove(repositoryId);
                    active.Dispose();
                }
            }
        }
    }

    private async Task SaveIfMissingAsync(
        Guid repositoryId,
        string repositoryPath,
        string commitHash,
        string path,
        CommitDiffViewMode viewMode,
        CancellationToken cancellationToken)
    {
        var cached = await _commitGraphRepository
            .GetCommitFileDiffAsync(repositoryId, commitHash, path, viewMode, cancellationToken)
            .ConfigureAwait(false);
        if (cached != null)
        {
            return;
        }

        var response = await BuildCommitFileDiffAsync(repositoryPath, commitHash, path, viewMode, cancellationToken)
            .ConfigureAwait(false);
        await _commitGraphRepository
            .SaveCommitFileDiffAsync(repositoryId, commitHash, path, response, cancellationToken)
            .ConfigureAwait(false);
    }

    private static async Task<CommitFileDiffResponse> BuildCommitFileDiffAsync(
        string repositoryPath,
        string commitHash,
        string path,
        CommitDiffViewMode viewMode,
        CancellationToken cancellationToken)
    {
        if (!GitObjectId.TryParse(commitHash, out var commitId))
        {
            throw new InvalidDataException("CommitHash is invalid.");
        }

        using var repository = await LovelyGitRepository.OpenAsync(repositoryPath, cancellationToken)
            .ConfigureAwait(false);
        var commit = await repository.GetCommitAsync(commitId, cancellationToken).ConfigureAwait(false);
        GitCommit? firstParent = null;
        if (commit.ParentHashes.Count > 0)
        {
            firstParent = await repository.GetCommitAsync(commit.ParentHashes[0], cancellationToken)
                .ConfigureAwait(false);
        }

        var comparison = await repository
            .GetChangedTreeFilesAsync(firstParent?.TreeHash, commit.TreeHash, cancellationToken)
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
            return new CommitFileDiffResponse
            {
                CommitHash = commitHash,
                Path = path,
                Status = status,
                ViewMode = viewMode,
                IsBinary = true,
                HasDifferences = true,
            };
        }

        var language = ResolveLanguage(path);
        return viewMode == CommitDiffViewMode.SideBySide
            ? BuildSideBySideResponse(commitHash, path, status, oldBlob.Text, newBlob.Text, language)
            : BuildCombinedResponse(commitHash, path, status, oldBlob.Text, newBlob.Text, language);
    }

    private static CommitFileDiffResponse BuildSideBySideResponse(
        string commitHash,
        string path,
        string status,
        string oldText,
        string newText,
        ILanguage? language)
    {
        var model = SideBySideDiffBuilder.Instance.BuildDiffModel(oldText, newText);
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
                OldSyntaxSpans = BuildSyntaxSpans(oldLineText, language),
                NewSyntaxSpans = BuildSyntaxSpans(newLineText, language),
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

    private static CommitFileDiffResponse BuildCombinedResponse(
        string commitHash,
        string path,
        string status,
        string oldText,
        string newText,
        ILanguage? language)
    {
        var model = InlineDiffBuilder.Instance.BuildDiffModel(oldText, newText);
        var oldLineNumber = 1;
        var newLineNumber = 1;
        var lines = new List<CommitFileDiffLine>(model.Lines.Count);

        foreach (var line in model.Lines)
        {
            var changeType = line.Type.ToString();
            int? oldLine = null;
            int? newLine = null;

            if (line.Type == ChangeType.Inserted)
            {
                newLine = newLineNumber++;
            }
            else if (line.Type == ChangeType.Deleted)
            {
                oldLine = oldLineNumber++;
            }
            else
            {
                oldLine = oldLineNumber++;
                newLine = newLineNumber++;
            }

            lines.Add(new CommitFileDiffLine
            {
                OldLineNumber = oldLine,
                NewLineNumber = newLine,
                Text = line.Text,
                ChangeType = changeType,
                SyntaxSpans = BuildSyntaxSpans(line.Text, language),
                ChangeSpans = BuildChangeSpans(line),
            });
        }

        return new CommitFileDiffResponse
        {
            CommitHash = commitHash,
            Path = path,
            Status = status,
            ViewMode = CommitDiffViewMode.Combined,
            IsBinary = false,
            HasDifferences = model.HasDifferences,
            Lines = lines,
        };
    }

    private static string GetStatus(GitTreeFile? oldFile, GitTreeFile? newFile)
    {
        if (oldFile == null)
        {
            return "Added";
        }

        if (newFile == null)
        {
            return "Deleted";
        }

        return oldFile.Mode == newFile.Mode ? "Modified" : "TypeChanged";
    }

    private static string GetSideBySideChangeType(DiffPiece? oldLine, DiffPiece? newLine)
    {
        if (oldLine?.Type == ChangeType.Deleted || newLine?.Type == ChangeType.Deleted)
        {
            return ChangeType.Deleted.ToString();
        }

        if (oldLine?.Type == ChangeType.Inserted || newLine?.Type == ChangeType.Inserted)
        {
            return ChangeType.Inserted.ToString();
        }

        if (oldLine?.Type == ChangeType.Modified || newLine?.Type == ChangeType.Modified)
        {
            return ChangeType.Modified.ToString();
        }

        if (oldLine?.Type == ChangeType.Imaginary || newLine?.Type == ChangeType.Imaginary)
        {
            return ChangeType.Imaginary.ToString();
        }

        return ChangeType.Unchanged.ToString();
    }

    private static ILanguage? ResolveLanguage(string path)
    {
        var extension = Path.GetExtension(path).ToLowerInvariant();
        return extension switch
        {
            ".cs" => Languages.CSharp,
            ".css" => Languages.Css,
            ".fs" or ".fsx" => Languages.FSharp,
            ".htm" or ".html" => Languages.Html,
            ".java" => Languages.Java,
            ".js" or ".jsx" or ".mjs" or ".cjs" => Languages.JavaScript,
            ".json" => Languages.JavaScript,
            ".md" or ".markdown" => Languages.Markdown,
            ".php" => Languages.Php,
            ".ps1" or ".psm1" or ".psd1" => Languages.PowerShell,
            ".py" => Languages.Python,
            ".sql" => Languages.Sql,
            ".ts" or ".tsx" => Languages.Typescript,
            ".vb" => Languages.VbDotNet,
            ".xml" or ".xaml" or ".csproj" or ".slnx" => Languages.Xml,
            _ => null,
        };
    }

    private static List<CommitFileDiffChangeSpan> BuildChangeSpans(DiffPiece? line)
    {
        if (line?.SubPieces == null || line.SubPieces.Count == 0)
        {
            if (line?.Type is ChangeType.Inserted or ChangeType.Deleted)
            {
                var lineText = line.Text ?? string.Empty;
                if (lineText.Length == 0)
                {
                    return new List<CommitFileDiffChangeSpan>();
                }

                return new List<CommitFileDiffChangeSpan>
                {
                    new()
                    {
                        Start = 0,
                        Length = lineText.Length,
                        ChangeType = line.Type.ToString(),
                    },
                };
            }

            return new List<CommitFileDiffChangeSpan>();
        }

        var spans = new List<CommitFileDiffChangeSpan>();
        var offset = 0;
        foreach (var piece in line.SubPieces)
        {
            var pieceText = piece.Text ?? string.Empty;
            if (piece.Type is ChangeType.Inserted or ChangeType.Deleted or ChangeType.Modified)
            {
                if (pieceText.Length > 0)
                {
                    spans.Add(new CommitFileDiffChangeSpan
                    {
                        Start = offset,
                        Length = pieceText.Length,
                        ChangeType = piece.Type.ToString(),
                    });
                }
            }

            offset += pieceText.Length;
        }

        return spans;
    }

    private static List<CommitFileDiffSyntaxSpan> BuildSyntaxSpans(string text, ILanguage? language)
    {
        if (language == null || string.IsNullOrEmpty(text))
        {
            return new List<CommitFileDiffSyntaxSpan>();
        }

        var spans = new List<CommitFileDiffSyntaxSpan>();
        var offset = 0;
        var repository = new LanguageRepository(new Dictionary<string, ILanguage>(StringComparer.OrdinalIgnoreCase));
        var compiler = new LanguageCompiler(
            new Dictionary<string, CompiledLanguage>(StringComparer.OrdinalIgnoreCase),
            new ReaderWriterLockSlim());
        var parser = new LanguageParser(compiler, repository);
        try
        {
            parser.Parse(text, language, (chunk, scopes) =>
            {
                foreach (var scope in scopes)
                {
                    spans.Add(new CommitFileDiffSyntaxSpan
                    {
                        Start = offset + scope.Index,
                        Length = scope.Length,
                        Scope = scope.Name,
                    });
                }

                offset += chunk.Length;
            });
        }
        catch
        {
            return new List<CommitFileDiffSyntaxSpan>();
        }

        return spans;
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(CommitFileDiffService));
        }
    }

    private sealed record ActivePreparation(
        string CommitHash,
        CancellationTokenSource CancellationTokenSource,
        Task Task) : IDisposable
    {
        public void Dispose()
        {
            CancellationTokenSource.Dispose();
        }
    }
}
