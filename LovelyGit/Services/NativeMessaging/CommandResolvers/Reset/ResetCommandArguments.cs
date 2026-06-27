using System.Text.Json.Serialization;
using ExpressThat.LovelyGit.Services.TypeGeneration;

namespace ExpressThat.LovelyGit.Services.NativeMessaging.CommandResolvers.Reset;

[TypeSharp]
[Union]
[JsonConverter(typeof(JsonStringEnumConverter<GitResetMode>))]
public enum GitResetMode
{
    Soft,
    Mixed,
    Hard,
}

[TypeSharp]
public sealed record ResetCurrentBranchToCommitCommandArguments(
    Guid RepositoryId,
    string CommitHash,
    GitResetMode ResetMode);
