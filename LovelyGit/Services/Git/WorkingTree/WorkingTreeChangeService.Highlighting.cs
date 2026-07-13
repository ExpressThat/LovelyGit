using System.Security.Cryptography;
using ColorCode;
using ExpressThat.LovelyGit.Services.Git.CommitGraph.Details;
using ExpressThat.LovelyGit.Services.Git.CommitGraph.Models;
using ExpressThat.LovelyGit.Services.Git.Diffing;

namespace ExpressThat.LovelyGit.Services.Git.WorkingTree;

internal sealed partial class WorkingTreeChangeService
{
    private static List<CommitFileDiffSyntaxSpan> BuildSyntaxSpans(
        string text,
        SyntaxSpanBuilder syntaxSpanBuilder) => syntaxSpanBuilder.BuildSpans(text);

    private static ILanguage? ResolveLanguage(string path) => Path.GetExtension(path).ToLowerInvariant() switch
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

    private static string[] SplitLines(byte[] bytes) => bytes.Length == 0
        ? []
        : System.Text.Encoding.UTF8.GetString(bytes)
            .Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');

    internal static bool IsBinary(byte[] bytes)
    {
        var length = Math.Min(bytes.Length, 8000);
        for (var index = 0; index < length; index++)
            if (bytes[index] == 0) return true;
        return false;
    }

    private static string NormalizePath(string path) => path.Replace('\\', '/').TrimStart('/');

    private static string FromGitPath(string path) => path.Replace('/', Path.DirectorySeparatorChar);

    private static bool IsSubmoduleMode(string mode) => string.Equals(mode, "160000", StringComparison.Ordinal);
}
