using System.Text.Json.Serialization;
using ExpressThat.LovelyGit.Services.Git.Configuration;
using ExpressThat.LovelyGit.Services.NativeMessaging.Commands;

namespace ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.Configuration;

[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(GetCommitIdentityCommandArguments))]
[JsonSerializable(typeof(ManageCommitIdentityCommandArguments))]
[JsonSerializable(typeof(GitIdentityValueSource))]
[JsonSerializable(typeof(GitCommitIdentity))]
[JsonSerializable(typeof(CommandResponse<GitCommitIdentity>))]
internal partial class ConfigurationJsonSerializerContext : JsonSerializerContext
{
}
