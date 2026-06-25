using System.Text.Json.Serialization;
using ExpressThat.LovelyGit.Services.NativeMessaging.Commands;
using ExpressThat.LovelyGit.Services.Git.WorkingTree.Models;

namespace ExpressThat.LovelyGit.Services.NativeMessaging;

[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(NativeMessageEnvelope))]
[JsonSerializable(typeof(NativeHostStatus))]
[JsonSerializable(typeof(NativeMessageRequest<System.Text.Json.JsonElement>), TypeInfoPropertyName = "NativeMessageRequestJsonElement")]
[JsonSerializable(typeof(NativeMessageResponse<System.Text.Json.JsonElement?>), TypeInfoPropertyName = "NativeMessageResponseJsonElement")]
[JsonSerializable(typeof(NativeMessageResponse<WorkingTreeChangedNotification>))]
[JsonSerializable(typeof(NativeMessageResponse<CommitGraphChangedNotification>))]
[JsonSerializable(typeof(NativeMessageResponse<object>))]
[JsonSerializable(typeof(string))]
internal partial class NativeMessagingJsonContext : JsonSerializerContext;
