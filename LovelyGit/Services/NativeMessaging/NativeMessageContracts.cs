using ExpressThat.LovelyGit.Services.TypeGeneration;

namespace ExpressThat.LovelyGit.Services.NativeMessaging;

[TypeSharp]
public sealed record NativeMessageRequest<TBody>(string MessageId, TBody Body);

[TypeSharp]
public sealed record NativeMessageResponse<TBody>(
    string MessageId,
    bool Success,
    TBody? Body,
    string? Error,
    NativeMessageMetrics? Metrics);

[TypeSharp]
public sealed record FrontendReadyRequest(string Source, string SentAt);
