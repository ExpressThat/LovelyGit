using ExpressThat.LovelyGit.Services.TypeGeneration;

namespace ExpressThat.LovelyGit.Services.NativeMessaging;

[TypeSharp]
public sealed record NativeHostStatus(
    string Message,
    [property: TypeAs("string")]
    DateTimeOffset SentAt);
