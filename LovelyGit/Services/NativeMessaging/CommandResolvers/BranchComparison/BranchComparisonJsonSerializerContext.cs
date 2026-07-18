using System.Text.Json.Serialization;
using ExpressThat.LovelyGit.Services.Git.BranchComparison;
using ExpressThat.LovelyGit.Services.NativeMessaging.Commands;

namespace ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.BranchComparison;

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(GetBranchComparisonCommandArguments))]
[JsonSerializable(typeof(List<BranchComparisonFile>))]
[JsonSerializable(typeof(CommandResponse<BranchComparisonResponse>))]
internal partial class BranchComparisonJsonSerializerContext : JsonSerializerContext;
