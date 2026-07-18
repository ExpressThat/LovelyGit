using ExpressThat.LovelyGit.Services.Git.CommitGraph.Models;
using ExpressThat.LovelyGit.Services.NativeMessaging.Commands;
using ExpressThat.LovelyGit.Services.Git.Reflog;
using ExpressThat.LovelyGit.Services.Git.CommitSearch;
using ExpressThat.LovelyGit.Services.Git.FileHistory;
using ExpressThat.LovelyGit.Services.Git.FileBlame;
using System.Text.Json.Serialization;

namespace ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.CommitGraph;

[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(CommitStats))]
[JsonSerializable(typeof(CommitInfo))]
[JsonSerializable(typeof(CommitRefInfo))]
[JsonSerializable(typeof(CommitRefKind))]
[JsonSerializable(typeof(CommitChangedFile))]
[JsonSerializable(typeof(CommitDetailsResponse))]
[JsonSerializable(typeof(CommitDiffViewMode))]
[JsonSerializable(typeof(CommitFileDiffSyntaxSpan))]
[JsonSerializable(typeof(CommitFileDiffChangeSpan))]
[JsonSerializable(typeof(CommitFileDiffLine))]
[JsonSerializable(typeof(CommitFileDiffResponse))]
[JsonSerializable(typeof(CommitPatchResponse))]
[JsonSerializable(typeof(CommitPatchExportResponse))]
[JsonSerializable(typeof(CommitPatchSeriesResponse))]
[JsonSerializable(typeof(CommitArchiveExportResponse))]
[JsonSerializable(typeof(CommitLaneColor))]
[JsonSerializable(typeof(CommitLaneEdge))]
[JsonSerializable(typeof(CommitGraphRow))]
[JsonSerializable(typeof(CommitGraphResponse))]
[JsonSerializable(typeof(RepositoryRefItem))]
[JsonSerializable(typeof(List<RepositoryRefItem>))]
[JsonSerializable(typeof(RepositoryWorktreeItem))]
[JsonSerializable(typeof(RepositoryBranchUpstreamItem))]
[JsonSerializable(typeof(RepositoryRefsResponse))]
[JsonSerializable(typeof(CommitGraphCommandArguments))]
[JsonSerializable(typeof(GetRepositoryRefsCommandArguments))]
[JsonSerializable(typeof(GetReflogCommandArguments))]
[JsonSerializable(typeof(GitReflogEntry))]
[JsonSerializable(typeof(GitReflogResponse))]
[JsonSerializable(typeof(SearchCommitsCommandArguments))]
[JsonSerializable(typeof(CommitSearchResult))]
[JsonSerializable(typeof(CommitSearchResponse))]
[JsonSerializable(typeof(GetFileHistoryCommandArguments))]
[JsonSerializable(typeof(FileHistoryChangeKind))]
[JsonSerializable(typeof(FileHistoryResult))]
[JsonSerializable(typeof(FileHistoryResponse))]
[JsonSerializable(typeof(GetFileBlameCommandArguments))]
[JsonSerializable(typeof(FileBlameHunk))]
[JsonSerializable(typeof(FileBlameResponse))]
[JsonSerializable(typeof(GetCommitDetailsCommandArguments))]
[JsonSerializable(typeof(GetCommitFileDiffCommandArguments))]
[JsonSerializable(typeof(GetCommitPatchCommandArguments))]
[JsonSerializable(typeof(CommitPatchSeriesCommandArguments))]
[JsonSerializable(typeof(CommandResponse<CommitGraphResponse>))]
[JsonSerializable(typeof(CommandResponse<RepositoryRefsResponse>))]
[JsonSerializable(typeof(CommandResponse<GitReflogResponse>))]
[JsonSerializable(typeof(CommandResponse<CommitSearchResponse>))]
[JsonSerializable(typeof(CommandResponse<FileHistoryResponse>))]
[JsonSerializable(typeof(CommandResponse<FileBlameResponse>))]
[JsonSerializable(typeof(CommandResponse<CommitDetailsResponse>))]
[JsonSerializable(typeof(CommandResponse<CommitFileDiffResponse>))]
[JsonSerializable(typeof(CommandResponse<CommitPatchResponse>))]
[JsonSerializable(typeof(CommandResponse<CommitPatchExportResponse>))]
[JsonSerializable(typeof(CommandResponse<CommitPatchSeriesResponse>))]
[JsonSerializable(typeof(CommandResponse<CommitArchiveExportResponse>))]
internal partial class CommitGraphJsonSerializerContext : JsonSerializerContext
{
}
