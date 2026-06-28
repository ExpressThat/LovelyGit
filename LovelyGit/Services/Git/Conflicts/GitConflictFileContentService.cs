using ColorCode;
using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser;
using ExpressThat.LovelyGit.Services.Git.WorkingTree;

namespace ExpressThat.LovelyGit.Services.Git.Conflicts;

internal sealed class GitConflictFileContentService
{
    private const int MaxFileBytes = 1_000_000;

    public async Task<GitConflictFileContentResponse> GetContentAsync(
        string repositoryPath,
        string path,
        CancellationToken cancellationToken)
    {
        path = NormalizePath(path);
        using var repository = await LovelyGitRepository
            .OpenAsync(repositoryPath, cancellationToken)
            .ConfigureAwait(false);
        var entries = await new GitIndexReader()
            .ReadAsync(repository.GitDirectory, repository.ObjectFormat, cancellationToken)
            .ConfigureAwait(false);
        var staged = entries
            .Where(entry => entry.Path == path)
            .ToDictionary(entry => entry.Stage);
        var oursBytes = await ReadStageAsync(repository, staged, 2, cancellationToken)
            .ConfigureAwait(false);
        var theirsBytes = await ReadStageAsync(repository, staged, 3, cancellationToken)
            .ConfigureAwait(false);
        var resultBytes = await ReadWorktreeAsync(repository.WorkTreeDirectory, path, cancellationToken)
            .ConfigureAwait(false);
        var isBinary = IsBinary(oursBytes) || IsBinary(theirsBytes) || IsBinary(resultBytes);
        if (isBinary)
        {
            return new GitConflictFileContentResponse { Path = path, IsBinary = true };
        }

        var language = ResolveLanguage(path);
        var resultText = Decode(resultBytes);
        return new GitConflictFileContentResponse
        {
            Path = path,
            ConflictCount = CountConflictMarkers(resultText),
            OursLines = BuildLines(Decode(oursBytes), language),
            TheirsLines = BuildLines(Decode(theirsBytes), language),
            ResultLines = BuildLines(resultText, language),
        };
    }

    private static async Task<byte[]> ReadStageAsync(
        LovelyGitRepository repository,
        Dictionary<int, GitIndexEntry> staged,
        int stage,
        CancellationToken cancellationToken)
    {
        if (!staged.TryGetValue(stage, out var entry) || IsSubmoduleMode(entry.Mode))
        {
            return [];
        }

        return await repository.ReadBlobAsync(entry.ObjectId, cancellationToken)
            .ConfigureAwait(false);
    }

    private static async Task<byte[]> ReadWorktreeAsync(
        string workTreeDirectory,
        string path,
        CancellationToken cancellationToken)
    {
        var fullPath = Path.Combine(workTreeDirectory, FromGitPath(path));
        if (!File.Exists(fullPath) || new FileInfo(fullPath).Length > MaxFileBytes)
        {
            return [];
        }

        return await File.ReadAllBytesAsync(fullPath, cancellationToken).ConfigureAwait(false);
    }

    private static List<GitConflictTextLine> BuildLines(string text, ILanguage? language)
    {
        var rawLines = text.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');
        var lines = new List<GitConflictTextLine>(rawLines.Length);
        var highlighter = ConflictSyntaxHighlighter.Create(language, text.Length);
        for (var index = 0; index < rawLines.Length; index++)
        {
            var line = rawLines[index];
            lines.Add(new GitConflictTextLine
            {
                LineNumber = index + 1,
                Text = line,
                MarkerKind = MarkerKind(line),
                SyntaxSpans = highlighter.BuildSpans(line),
            });
        }

        return lines;
    }

    private static ILanguage? ResolveLanguage(string path) =>
        Path.GetExtension(path).ToLowerInvariant() switch
        {
            ".cs" => Languages.CSharp,
            ".css" => Languages.Css,
            ".js" or ".jsx" or ".mjs" or ".cjs" => Languages.JavaScript,
            ".json" => Languages.JavaScript,
            ".md" or ".markdown" => Languages.Markdown,
            ".ps1" or ".psm1" or ".psd1" => Languages.PowerShell,
            ".py" => Languages.Python,
            ".ts" or ".tsx" => Languages.Typescript,
            ".xml" or ".xaml" or ".csproj" or ".slnx" => Languages.Xml,
            _ => null,
        };

    private static string MarkerKind(string line)
    {
        if (line.StartsWith("<<<<<<<", StringComparison.Ordinal)) return "OursStart";
        if (line.StartsWith("=======", StringComparison.Ordinal)) return "Divider";
        return line.StartsWith(">>>>>>>", StringComparison.Ordinal) ? "TheirsEnd" : string.Empty;
    }

    private static bool IsBinary(byte[] bytes) =>
        bytes.Take(Math.Min(bytes.Length, 8000)).Any(value => value == 0);

    private static int CountConflictMarkers(string text) =>
        text.Split('\n').Count(line => line.StartsWith("<<<<<<<", StringComparison.Ordinal));

    private static string Decode(byte[] bytes) =>
        bytes.Length == 0 ? string.Empty : System.Text.Encoding.UTF8.GetString(bytes);

    private static bool IsSubmoduleMode(string mode) => mode == "160000";

    private static string NormalizePath(string path) => path.Replace('\\', '/').TrimStart('/');

    private static string FromGitPath(string path) => path.Replace('/', Path.DirectorySeparatorChar);
}
