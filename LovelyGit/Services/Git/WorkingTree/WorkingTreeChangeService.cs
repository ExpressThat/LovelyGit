using System.Security.Cryptography;
using ColorCode;
using ColorCode.Common;
using ColorCode.Compilation;
using ColorCode.Parsing;
using DiffPlex;
using DiffPlex.DiffBuilder;
using DiffPlex.DiffBuilder.Model;
using ExpressThat.LovelyGit.Services.Git.CommitGraph.Models;
using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser;
using ExpressThat.LovelyGit.Services.Git.WorkingTree.Models;

namespace ExpressThat.LovelyGit.Services.Git.WorkingTree;

internal sealed class WorkingTreeChangeService
{
    private const int MaxSyntaxHighlightedCharacters = 750_000;
    private const int MaxSyntaxHighlightedLineLength = 2_000;

    public async Task<WorkingTreeChangesResponse> GetChangesAsync(
        string repositoryPath,
        CancellationToken cancellationToken)
    {
        using var repository = await LovelyGitRepository.OpenAsync(repositoryPath, cancellationToken).ConfigureAwait(false);
        var indexEntries = await new GitIndexReader()
            .ReadAsync(repository.GitDirectory, repository.ObjectFormat, cancellationToken)
            .ConfigureAwait(false);
        var normalIndexEntries = indexEntries
            .Where(entry => entry.Stage == 0 && !entry.SkipWorkTree && !entry.IntentToAdd)
            .ToDictionary(entry => entry.Path, StringComparer.Ordinal);
        var unmerged = indexEntries
            .Where(entry => entry.Stage != 0)
            .GroupBy(entry => entry.Path, StringComparer.Ordinal)
            .Select(group => new WorkingTreeChangedFile
            {
                Path = group.Key,
                Status = "Unmerged",
                Group = WorkingTreeChangeGroup.Unmerged,
            })
            .OrderBy(file => file.Path, StringComparer.Ordinal)
            .ToList();

        var headFiles = await ReadHeadFilesAsync(repository, cancellationToken).ConfigureAwait(false);
        var staged = await BuildStagedChangesAsync(repository, headFiles, normalIndexEntries, cancellationToken)
            .ConfigureAwait(false);
        var unstaged = await BuildUnstagedChangesAsync(repository, normalIndexEntries, cancellationToken)
            .ConfigureAwait(false);
        var untracked = await BuildUntrackedChangesAsync(repository, normalIndexEntries.Keys, cancellationToken)
            .ConfigureAwait(false);

        return new WorkingTreeChangesResponse
        {
            Staged = staged,
            Unstaged = unstaged,
            Untracked = untracked,
            Unmerged = unmerged,
        };
    }

    public async Task<CommitFileDiffResponse> GetFileDiffAsync(
        string repositoryPath,
        string path,
        WorkingTreeChangeGroup group,
        CommitDiffViewMode viewMode,
        CancellationToken cancellationToken)
    {
        path = NormalizePath(path);
        using var repository = await LovelyGitRepository.OpenAsync(repositoryPath, cancellationToken).ConfigureAwait(false);
        var indexEntries = await new GitIndexReader()
            .ReadAsync(repository.GitDirectory, repository.ObjectFormat, cancellationToken)
            .ConfigureAwait(false);
        var indexByPath = indexEntries
            .Where(entry => entry.Stage == 0)
            .ToDictionary(entry => entry.Path, StringComparer.Ordinal);
        var headFiles = await ReadHeadFilesAsync(repository, cancellationToken).ConfigureAwait(false);

        byte[] oldBytes = Array.Empty<byte>();
        byte[] newBytes = Array.Empty<byte>();
        var status = "Modified";

        if (group == WorkingTreeChangeGroup.Staged)
        {
            headFiles.TryGetValue(path, out var headFile);
            indexByPath.TryGetValue(path, out var indexEntry);
            oldBytes = headFile == null
                ? Array.Empty<byte>()
                : await TryReadBlobBytesAsync(repository, headFile.ObjectId, headFile.Mode, cancellationToken).ConfigureAwait(false) ?? Array.Empty<byte>();
            newBytes = indexEntry == null
                ? Array.Empty<byte>()
                : await TryReadBlobBytesAsync(repository, indexEntry.ObjectId, indexEntry.Mode, cancellationToken).ConfigureAwait(false) ?? Array.Empty<byte>();
            status = headFile == null ? "Added" : indexEntry == null ? "Deleted" : "Modified";
        }
        else if (group == WorkingTreeChangeGroup.Unstaged)
        {
            if (!indexByPath.TryGetValue(path, out var indexEntry))
            {
                throw new FileNotFoundException("Index entry not found.", path);
            }

            oldBytes = await TryReadBlobBytesAsync(repository, indexEntry.ObjectId, indexEntry.Mode, cancellationToken).ConfigureAwait(false) ?? Array.Empty<byte>();
            var worktreePath = Path.Combine(repository.WorkTreeDirectory, FromGitPath(path));
            newBytes = File.Exists(worktreePath)
                ? await File.ReadAllBytesAsync(worktreePath, cancellationToken).ConfigureAwait(false)
                : Array.Empty<byte>();
            status = File.Exists(worktreePath) ? "Modified" : "Deleted";
        }
        else if (group == WorkingTreeChangeGroup.Untracked)
        {
            var worktreePath = Path.Combine(repository.WorkTreeDirectory, FromGitPath(path));
            if (indexByPath.TryGetValue(path, out var indexEntry))
            {
                oldBytes = await TryReadBlobBytesAsync(repository, indexEntry.ObjectId, indexEntry.Mode, cancellationToken).ConfigureAwait(false) ?? Array.Empty<byte>();
                newBytes = File.Exists(worktreePath)
                    ? await File.ReadAllBytesAsync(worktreePath, cancellationToken).ConfigureAwait(false)
                    : Array.Empty<byte>();
                status = File.Exists(worktreePath) ? "Modified" : "Deleted";
                return BuildDiffResponse("WORKTREE", path, status, viewMode, oldBytes, newBytes);
            }

            newBytes = await File.ReadAllBytesAsync(worktreePath, cancellationToken).ConfigureAwait(false);
            status = "Added";
        }
        else
        {
            throw new InvalidOperationException("Unmerged file diffs are not available yet.");
        }

        return BuildDiffResponse("WORKTREE", path, status, viewMode, oldBytes, newBytes);
    }

    private async Task<Dictionary<string, GitTreeFile>> ReadHeadFilesAsync(
        LovelyGitRepository repository,
        CancellationToken cancellationToken)
    {
        if (repository.HeadTarget == null)
        {
            return new Dictionary<string, GitTreeFile>(StringComparer.Ordinal);
        }

        var head = await repository.GetCommitAsync(repository.HeadTarget.Value, cancellationToken).ConfigureAwait(false);
        var comparison = await repository
            .GetChangedTreeFilesAsync(null, head.TreeHash, cancellationToken)
            .ConfigureAwait(false);
        return new Dictionary<string, GitTreeFile>(comparison.CurrentFiles, StringComparer.Ordinal);
    }

    private async Task<List<WorkingTreeChangedFile>> BuildStagedChangesAsync(
        LovelyGitRepository repository,
        IReadOnlyDictionary<string, GitTreeFile> headFiles,
        IReadOnlyDictionary<string, GitIndexEntry> indexEntries,
        CancellationToken cancellationToken)
    {
        var paths = headFiles.Keys.Concat(indexEntries.Keys).Distinct(StringComparer.Ordinal);
        var changes = new List<WorkingTreeChangedFile>();
        foreach (var path in paths)
        {
            cancellationToken.ThrowIfCancellationRequested();
            headFiles.TryGetValue(path, out var headFile);
            indexEntries.TryGetValue(path, out var indexEntry);
            if (headFile?.ObjectId == indexEntry?.ObjectId && headFile?.Mode == indexEntry?.Mode)
            {
                continue;
            }

            var oldBytes = headFile == null
                ? Array.Empty<byte>()
                : await TryReadBlobBytesAsync(repository, headFile.ObjectId, headFile.Mode, cancellationToken).ConfigureAwait(false);
            var newBytes = indexEntry == null
                ? Array.Empty<byte>()
                : await TryReadBlobBytesAsync(repository, indexEntry.ObjectId, indexEntry.Mode, cancellationToken).ConfigureAwait(false);
            var stats = CalculateStats(oldBytes, newBytes);
            changes.Add(new WorkingTreeChangedFile
            {
                Path = path,
                Status = headFile == null ? "Added" : indexEntry == null ? "Deleted" : "Modified",
                Group = WorkingTreeChangeGroup.Staged,
                Additions = stats.Additions,
                Deletions = stats.Deletions,
                IsBinary = stats.IsBinary,
            });
        }

        return changes.OrderBy(file => file.Path, StringComparer.Ordinal).ToList();
    }

    private async Task<List<WorkingTreeChangedFile>> BuildUnstagedChangesAsync(
        LovelyGitRepository repository,
        IReadOnlyDictionary<string, GitIndexEntry> indexEntries,
        CancellationToken cancellationToken)
    {
        var changes = new List<WorkingTreeChangedFile>();
        foreach (var entry in indexEntries.Values)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var path = Path.Combine(repository.WorkTreeDirectory, FromGitPath(entry.Path));
            if (!File.Exists(path))
            {
                var deletedOldBytes = await TryReadBlobBytesAsync(repository, entry.ObjectId, entry.Mode, cancellationToken).ConfigureAwait(false);
                var deletedStats = CalculateStats(deletedOldBytes, Array.Empty<byte>());
                changes.Add(new WorkingTreeChangedFile
                {
                    Path = entry.Path,
                    Status = "Deleted",
                    Group = WorkingTreeChangeGroup.Unstaged,
                    Additions = deletedStats.Additions,
                    Deletions = deletedStats.Deletions,
                    IsBinary = deletedStats.IsBinary,
                });
                continue;
            }

            var info = new FileInfo(path);
            if (entry.FileSize == info.Length
                && Math.Abs((info.LastWriteTimeUtc - entry.ModifiedTime.UtcDateTime).TotalSeconds) < 1)
            {
                continue;
            }

            var newBytes = await File.ReadAllBytesAsync(path, cancellationToken).ConfigureAwait(false);
            var newObjectId = ComputeBlobObjectId(newBytes, repository.ObjectFormat);
            if (newObjectId == entry.ObjectId)
            {
                continue;
            }

            var oldBytes = await TryReadBlobBytesAsync(repository, entry.ObjectId, entry.Mode, cancellationToken).ConfigureAwait(false);
            var stats = CalculateStats(oldBytes, newBytes);
            changes.Add(new WorkingTreeChangedFile
            {
                Path = entry.Path,
                Status = "Modified",
                Group = WorkingTreeChangeGroup.Unstaged,
                Additions = stats.Additions,
                Deletions = stats.Deletions,
                IsBinary = stats.IsBinary,
            });
        }

        return changes.OrderBy(file => file.Path, StringComparer.Ordinal).ToList();
    }

    private async Task<List<WorkingTreeChangedFile>> BuildUntrackedChangesAsync(
        LovelyGitRepository repository,
        IEnumerable<string> trackedPaths,
        CancellationToken cancellationToken)
    {
        var tracked = new HashSet<string>(trackedPaths, StringComparer.Ordinal);
        var matcher = await GitIgnoreMatcher
            .LoadAsync(repository.WorkTreeDirectory, repository.GitDirectory, cancellationToken)
            .ConfigureAwait(false);
        var changes = new List<WorkingTreeChangedFile>();
        var pendingDirectories = new Stack<string>();
        pendingDirectories.Push(repository.WorkTreeDirectory);

        while (pendingDirectories.Count > 0)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var directory = pendingDirectories.Pop();
            var relativeDirectory = NormalizePath(Path.GetRelativePath(repository.WorkTreeDirectory, directory));
            if (relativeDirectory == ".")
            {
                relativeDirectory = string.Empty;
            }

            if (!string.IsNullOrEmpty(relativeDirectory))
            {
                await matcher
                    .LoadRulesForDirectoryAsync(repository.WorkTreeDirectory, relativeDirectory, cancellationToken)
                    .ConfigureAwait(false);
            }

            foreach (var childDirectory in SafeEnumerateDirectories(directory))
            {
                cancellationToken.ThrowIfCancellationRequested();
                var relative = NormalizePath(Path.GetRelativePath(repository.WorkTreeDirectory, childDirectory));
                if (relative.Equals(".git", StringComparison.Ordinal)
                    || relative.StartsWith(".git/", StringComparison.Ordinal)
                    || matcher.IsIgnored(relative, true))
                {
                    continue;
                }

                pendingDirectories.Push(childDirectory);
            }

            foreach (var filePath in SafeEnumerateFiles(directory))
            {
                cancellationToken.ThrowIfCancellationRequested();
                var relative = NormalizePath(Path.GetRelativePath(repository.WorkTreeDirectory, filePath));
                if (relative.StartsWith(".git/", StringComparison.Ordinal)
                    || tracked.Contains(relative)
                    || matcher.IsIgnored(relative, false))
                {
                    continue;
                }

                changes.Add(new WorkingTreeChangedFile
                {
                    Path = relative,
                    Status = "Added",
                    Group = WorkingTreeChangeGroup.Untracked,
                    Additions = 0,
                    Deletions = 0,
                    IsBinary = false,
                });
            }
        }

        return changes.OrderBy(file => file.Path, StringComparer.Ordinal).ToList();
    }

    private static IEnumerable<string> SafeEnumerateDirectories(string path)
    {
        try
        {
            return Directory.EnumerateDirectories(path);
        }
        catch
        {
            return Array.Empty<string>();
        }
    }

    private static IEnumerable<string> SafeEnumerateFiles(string path)
    {
        try
        {
            return Directory.EnumerateFiles(path);
        }
        catch
        {
            return Array.Empty<string>();
        }
    }

    private static GitObjectId ComputeBlobObjectId(byte[] bytes, GitObjectFormat objectFormat)
    {
        var header = System.Text.Encoding.ASCII.GetBytes($"blob {bytes.Length}\0");
        var combined = new byte[header.Length + bytes.Length];
        Buffer.BlockCopy(header, 0, combined, 0, header.Length);
        Buffer.BlockCopy(bytes, 0, combined, header.Length, bytes.Length);
        var hash = objectFormat == GitObjectFormat.Sha256 ? SHA256.HashData(combined) : SHA1.HashData(combined);
        return new GitObjectId(Convert.ToHexString(hash).ToLowerInvariant(), objectFormat);
    }

    private static async Task<byte[]?> TryReadBlobBytesAsync(
        LovelyGitRepository repository,
        GitObjectId objectId,
        string mode,
        CancellationToken cancellationToken)
    {
        if (IsSubmoduleMode(mode))
        {
            return null;
        }

        try
        {
            return await repository.ReadBlobAsync(objectId, cancellationToken).ConfigureAwait(false);
        }
        catch when (!cancellationToken.IsCancellationRequested)
        {
            return null;
        }
    }

    private static (uint Additions, uint Deletions, bool IsBinary) CalculateStats(byte[]? oldBytes, byte[]? newBytes)
    {
        if (oldBytes == null || newBytes == null)
        {
            return (0, 0, true);
        }

        var oldBinary = IsBinary(oldBytes);
        var newBinary = IsBinary(newBytes);
        if (oldBinary || newBinary)
        {
            return (0, 0, true);
        }

        var oldLines = SplitLines(oldBytes);
        var newLines = SplitLines(newBytes);
        var common = CountCommonLines(oldLines, newLines);
        return ((uint)(newLines.Length - common), (uint)(oldLines.Length - common), false);
    }

    private static int CountCommonLines(string[] oldLines, string[] newLines)
    {
        var counts = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var line in oldLines)
        {
            counts.TryGetValue(line, out var count);
            counts[line] = count + 1;
        }

        var common = 0;
        foreach (var line in newLines)
        {
            if (!counts.TryGetValue(line, out var count) || count == 0)
            {
                continue;
            }

            common++;
            if (count == 1)
            {
                counts.Remove(line);
            }
            else
            {
                counts[line] = count - 1;
            }
        }

        return common;
    }

    private static CommitFileDiffResponse BuildDiffResponse(
        string commitHash,
        string path,
        string status,
        CommitDiffViewMode viewMode,
        byte[] oldBytes,
        byte[] newBytes)
    {
        var isBinary = IsBinary(oldBytes) || IsBinary(newBytes);
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

        var oldText = System.Text.Encoding.UTF8.GetString(oldBytes);
        var newText = System.Text.Encoding.UTF8.GetString(newBytes);
        var language = oldText.Length + newText.Length <= MaxSyntaxHighlightedCharacters
            ? ResolveLanguage(path)
            : null;

        return viewMode == CommitDiffViewMode.SideBySide
            ? BuildSideBySideResponse(commitHash, path, status, oldText, newText, language)
            : BuildCombinedResponse(commitHash, path, status, oldText, newText, language);
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
                ChangeType = line.Type.ToString(),
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

    private static List<CommitFileDiffChangeSpan> BuildChangeSpans(DiffPiece? line)
    {
        if (line?.SubPieces == null || line.SubPieces.Count == 0)
        {
            if (line?.Type is ChangeType.Inserted or ChangeType.Deleted)
            {
                var lineText = line.Text ?? string.Empty;
                return lineText.Length == 0
                    ? new List<CommitFileDiffChangeSpan>()
                    : new List<CommitFileDiffChangeSpan>
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
            if (piece.Type is ChangeType.Inserted or ChangeType.Deleted or ChangeType.Modified && pieceText.Length > 0)
            {
                spans.Add(new CommitFileDiffChangeSpan
                {
                    Start = offset,
                    Length = pieceText.Length,
                    ChangeType = piece.Type.ToString(),
                });
            }

            offset += pieceText.Length;
        }

        return spans;
    }

    private static List<CommitFileDiffSyntaxSpan> BuildSyntaxSpans(string text, ILanguage? language)
    {
        if (language == null || string.IsNullOrEmpty(text) || text.Length > MaxSyntaxHighlightedLineLength)
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

    private static ILanguage? ResolveLanguage(string path)
    {
        return Path.GetExtension(path).ToLowerInvariant() switch
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

    private static string[] SplitLines(byte[] bytes)
    {
        if (bytes.Length == 0)
        {
            return Array.Empty<string>();
        }

        return System.Text.Encoding.UTF8.GetString(bytes).Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');
    }

    private static bool IsBinary(byte[] bytes)
    {
        var length = Math.Min(bytes.Length, 8000);
        for (var i = 0; i < length; i++)
        {
            if (bytes[i] == 0)
            {
                return true;
            }
        }

        return false;
    }

    private static string NormalizePath(string path)
    {
        return path.Replace('\\', '/').TrimStart('/');
    }

    private static string FromGitPath(string path)
    {
        return path.Replace('/', Path.DirectorySeparatorChar);
    }

    private static bool IsSubmoduleMode(string mode)
    {
        return string.Equals(mode, "160000", StringComparison.Ordinal);
    }
}
