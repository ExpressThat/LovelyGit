using System.Text.Json.Serialization.Metadata;

namespace ExpressThat.LovelyGit.Services.NativeMessaging;

internal interface INativeMessaging
{
    bool HasWindow { get; }

    void Send<TBody>(
        NativeMessageType messageType,
        NativeMessageResponse<TBody> response,
        JsonTypeInfo<NativeMessageResponse<TBody>> jsonTypeInfo);

    void Send<TBody>(
        NativeMessageType messageType,
        TBody body,
        JsonTypeInfo<NativeMessageResponse<TBody>> jsonTypeInfo);
}
