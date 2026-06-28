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

internal sealed partial class WorkingTreeChangeService
{
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

    private static CommitFileDiffResponse BuildUnreadableFileDiff(
        string commitHash,
        string path,
        string status,
        CommitDiffViewMode viewMode)
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

    private static CommitFileDiffResponse BuildCombinedResponse(
        string commitHash,
        string path,
        string status,
        string oldText,
        string newText,
        ILanguage? language,
        bool ignoreWhitespace)
    {
        var model = new InlineDiffBuilder(new Differ()).BuildDiffModel(oldText, newText, ignoreWhitespace);
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

}
