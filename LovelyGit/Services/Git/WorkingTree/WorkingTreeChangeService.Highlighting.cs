using System.Security.Cryptography;
using ColorCode;
using DiffPlex;
using DiffPlex.DiffBuilder;
using DiffPlex.DiffBuilder.Model;
using ExpressThat.LovelyGit.Services.Git.CommitGraph.Models;
using ExpressThat.LovelyGit.Services.Git.Diffing;
using ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser;
using ExpressThat.LovelyGit.Services.Git.WorkingTree.Models;

namespace ExpressThat.LovelyGit.Services.Git.WorkingTree;

internal sealed partial class WorkingTreeChangeService
{
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

    private static List<CommitFileDiffSyntaxSpan> BuildSyntaxSpans(
        string text,
        SyntaxSpanBuilder syntaxSpanBuilder)
    {
        return syntaxSpanBuilder.BuildSpans(text);
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

    internal static bool IsBinary(byte[] bytes)
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
