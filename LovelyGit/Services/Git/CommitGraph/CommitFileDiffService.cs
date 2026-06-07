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
using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser;

namespace ExpressThat.LovelyGit.Services.Git.CommitGraph;

internal sealed class CommitFileDiffService : IDisposable
{
    private const int MaxSyntaxHighlightedCharacters = 750_000;
    private const int MaxSyntaxHighlightedLineLength = 2_000;
    private readonly CommitGraphRepository _commitGraphRepository;
    private readonly object _preparationLock = new();
    private readonly object _diffBuildGateLock = new();
    private readonly Dictionary<Guid, ActivePreparation> _activePreparations = new();
    private readonly Dictionary<string, BuildGate> _diffBuildGates = new(StringComparer.Ordinal);
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
                var previousPreparation = previous;
                var task = Task.Run(
                    async () =>
                    {
                        if (previousPreparation != null)
                        {
                            try
                            {
                                await previousPreparation.Task.ConfigureAwait(false);
                            }
                            finally
                            {
                                previousPreparation.Dispose();
                            }
                        }

                        await PrepareCommitDiffsAsync(
                                repositoryId,
                                repositoryPath,
                                commitHash,
                                changedFiles,
                                previousPreparation?.CommitHash,
                                cancellationTokenSource.Token)
                            .ConfigureAwait(false);
                    },
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
    }

    public async Task<CommitFileDiffResponse> GetCommitFileDiffAsync(
        Guid repositoryId,
        string repositoryPath,
        string commitHash,
        string path,
        CommitDiffViewMode viewMode,
        CancellationToken cancellationToken)
    {
        var cached = await TryGetCachedDiffAsync(repositoryId, commitHash, path, viewMode, cancellationToken)
            .ConfigureAwait(false);
        if (cached != null)
        {
            return cached;
        }

        return await BuildAndCacheMissingDiffAsync(
                repositoryId,
                repositoryPath,
                commitHash,
                path,
                viewMode,
                cancellationToken)
            .ConfigureAwait(false);
    }

    public void CancelPreparingCommitDiffs(Guid repositoryId, string commitHash)
    {
        ActivePreparation? active = null;
        lock (_preparationLock)
        {
            if (_activePreparations.TryGetValue(repositoryId, out active)
                && string.Equals(active.CommitHash, commitHash, StringComparison.Ordinal))
            {
                _activePreparations.Remove(repositoryId);
                active.CancellationTokenSource.Cancel();
            }
            else
            {
                active = null;
            }
        }

        if (active != null)
        {
            _ = active.Task.ContinueWith(
                static (task, state) => ((ActivePreparation)state!).Dispose(),
                active,
                CancellationToken.None,
                TaskContinuationOptions.ExecuteSynchronously,
                TaskScheduler.Default);
        }
    }

    public async Task CancelRepositoryPreparationAsync(Guid repositoryId, CancellationToken cancellationToken)
    {
        ActivePreparation? active = null;
        lock (_preparationLock)
        {
            if (_activePreparations.Remove(repositoryId, out active))
            {
                active.CancellationTokenSource.Cancel();
            }
        }

        if (active == null)
        {
            return;
        }

        try
        {
            await active.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
        }
        catch
        {
        }
        finally
        {
            active.Dispose();
        }
    }

    public void Dispose()
    {
        StopAndWait();
    }

    public void StopAndWait()
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
        }

        try
        {
            Task.WaitAll(active.Select(preparation => preparation.Task).ToArray(), TimeSpan.FromSeconds(5));
        }
        catch
        {
        }

        foreach (var preparation in active)
        {
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

            await Parallel.ForEachAsync(changedFiles, cancellationToken, async (file, fileCancellationToken) =>
            {
                fileCancellationToken.ThrowIfCancellationRequested();

                await SaveMissingViewModesAsync(
                        repositoryId,
                        repositoryPath,
                        commitHash,
                        file.Path,
                        fileCancellationToken)
                    .ConfigureAwait(false);
            }).ConfigureAwait(false);
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

    private async Task SaveMissingViewModesAsync(
        Guid repositoryId,
        string repositoryPath,
        string commitHash,
        string path,
        CancellationToken cancellationToken)
    {
        var hasSideBySide = await _commitGraphRepository
            .HasCommitFileDiffAsync(repositoryId, commitHash, path, CommitDiffViewMode.SideBySide, cancellationToken)
            .ConfigureAwait(false);
        var hasCombined = await _commitGraphRepository
            .HasCommitFileDiffAsync(repositoryId, commitHash, path, CommitDiffViewMode.Combined, cancellationToken)
            .ConfigureAwait(false);

        if (hasSideBySide && hasCombined)
        {
            return;
        }

        var source = await BuildCommitFileDiffSourceAsync(repositoryPath, commitHash, path, cancellationToken)
            .ConfigureAwait(false);

        if (!hasSideBySide)
        {
            await BuildAndCacheMissingDiffAsync(
                    repositoryId,
                    repositoryPath,
                    commitHash,
                    path,
                    CommitDiffViewMode.SideBySide,
                    source,
                    cancellationToken)
                .ConfigureAwait(false);
        }

        if (!hasCombined)
        {
            await BuildAndCacheMissingDiffAsync(
                    repositoryId,
                    repositoryPath,
                    commitHash,
                    path,
                    CommitDiffViewMode.Combined,
                    source,
                    cancellationToken)
                .ConfigureAwait(false);
        }
    }

    private async Task<CommitFileDiffResponse> BuildAndCacheMissingDiffAsync(
        Guid repositoryId,
        string repositoryPath,
        string commitHash,
        string path,
        CommitDiffViewMode viewMode,
        CancellationToken cancellationToken)
    {
        var gateKey = MakeDiffGateKey(repositoryId, commitHash, path, viewMode);
        var gate = GetBuildGate(gateKey);
        var enteredGate = false;
        try
        {
            await gate.Semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            enteredGate = true;

            var cached = await TryGetCachedDiffAsync(repositoryId, commitHash, path, viewMode, cancellationToken)
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

            return await TryGetCachedDiffAsync(repositoryId, commitHash, path, viewMode, cancellationToken)
                .ConfigureAwait(false) ?? response;
        }
        finally
        {
            if (enteredGate)
            {
                gate.Semaphore.Release();
            }

            ReleaseBuildGate(gateKey, gate);
        }
    }

    private async Task<CommitFileDiffResponse> BuildAndCacheMissingDiffAsync(
        Guid repositoryId,
        string repositoryPath,
        string commitHash,
        string path,
        CommitDiffViewMode viewMode,
        CommitFileDiffSource source,
        CancellationToken cancellationToken)
    {
        var gateKey = MakeDiffGateKey(repositoryId, commitHash, path, viewMode);
        var gate = GetBuildGate(gateKey);
        var enteredGate = false;
        try
        {
            await gate.Semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            enteredGate = true;

            var cached = await TryGetCachedDiffAsync(repositoryId, commitHash, path, viewMode, cancellationToken)
                .ConfigureAwait(false);
            if (cached != null)
            {
                return cached;
            }

            var response = BuildResponseFromSource(commitHash, path, viewMode, source);
            await _commitGraphRepository
                .SaveCommitFileDiffAsync(repositoryId, commitHash, path, response, cancellationToken)
                .ConfigureAwait(false);

            return await TryGetCachedDiffAsync(repositoryId, commitHash, path, viewMode, cancellationToken)
                .ConfigureAwait(false) ?? response;
        }
        finally
        {
            if (enteredGate)
            {
                gate.Semaphore.Release();
            }

            ReleaseBuildGate(gateKey, gate);
        }
    }

    private static async Task<CommitFileDiffResponse> BuildCommitFileDiffAsync(
        string repositoryPath,
        string commitHash,
        string path,
        CommitDiffViewMode viewMode,
        CancellationToken cancellationToken)
    {
        var source = await BuildCommitFileDiffSourceAsync(repositoryPath, commitHash, path, cancellationToken)
            .ConfigureAwait(false);
        return BuildResponseFromSource(commitHash, path, viewMode, source);
    }

    private async Task<CommitFileDiffResponse?> TryGetCachedDiffAsync(
        Guid repositoryId,
        string commitHash,
        string path,
        CommitDiffViewMode viewMode,
        CancellationToken cancellationToken)
    {
        try
        {
            return await _commitGraphRepository
                .GetCommitFileDiffAsync(repositoryId, commitHash, path, viewMode, cancellationToken)
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

        return viewMode == CommitDiffViewMode.SideBySide
            ? BuildSideBySideResponse(commitHash, path, source.Status, source.OldText, source.NewText, source.Language)
            : BuildCombinedResponse(commitHash, path, source.Status, source.OldText, source.NewText, source.Language);
    }

    private static CommitFileDiffResponse BuildSideBySideResponse(
        string commitHash,
        string path,
        string status,
        string oldText,
        string newText,
        ILanguage? language)
    {
        var model = new SideBySideDiffBuilder(new Differ()).BuildDiffModel(oldText, newText);
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
        var model = new InlineDiffBuilder(new Differ()).BuildDiffModel(oldText, newText);
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

    private static string MakeDiffGateKey(
        Guid repositoryId,
        string commitHash,
        string path,
        CommitDiffViewMode viewMode)
    {
        return string.Concat(
            repositoryId.ToString("N"),
            ':',
            commitHash,
            ':',
            viewMode.ToString(),
            ':',
            path);
    }

    private BuildGate GetBuildGate(string key)
    {
        lock (_diffBuildGateLock)
        {
            if (!_diffBuildGates.TryGetValue(key, out var gate))
            {
                gate = new BuildGate();
                _diffBuildGates[key] = gate;
            }

            gate.ReferenceCount++;
            return gate;
        }
    }

    private void ReleaseBuildGate(string key, BuildGate gate)
    {
        lock (_diffBuildGateLock)
        {
            gate.ReferenceCount--;
            if (gate.ReferenceCount == 0
                && _diffBuildGates.TryGetValue(key, out var activeGate)
                && ReferenceEquals(activeGate, gate))
            {
                _diffBuildGates.Remove(key);
                gate.Semaphore.Dispose();
            }
        }
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
        if (language == null
            || string.IsNullOrEmpty(text)
            || text.Length > MaxSyntaxHighlightedLineLength)
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

    private sealed class BuildGate
    {
        public SemaphoreSlim Semaphore { get; } = new(1, 1);

        public int ReferenceCount { get; set; }
    }

    private sealed record CommitFileDiffSource
    {
        public string Status { get; init; } = string.Empty;
        public bool IsBinary { get; init; }
        public string OldText { get; init; } = string.Empty;
        public string NewText { get; init; } = string.Empty;
        public ILanguage? Language { get; init; }
    }
}
