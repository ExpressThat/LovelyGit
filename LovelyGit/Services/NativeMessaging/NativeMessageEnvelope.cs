using System.Text.Json;

namespace ExpressThat.LovelyGit.Services.NativeMessaging;

internal sealed record NativeMessageEnvelope(
    string Id,
    string Command,
    JsonElement? Data,
    int Version,
    string? RequestId,
    string? Channel);
