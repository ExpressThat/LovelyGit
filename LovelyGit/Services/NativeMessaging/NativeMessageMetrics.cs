using ExpressThat.LovelyGit.Services.TypeGeneration;

namespace ExpressThat.LovelyGit.Services.NativeMessaging;

[TypeSharp]
public sealed record NativeMessageMetrics(
    long DurationMs,
    int RequestPayloadBytes,
    long ManagedMemoryBytes,
    long WorkingSetBytes,
    long PrivateMemoryBytes);
