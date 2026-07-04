using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using ExpressThat.LovelyGit.Services.Diagnostics;
using InfiniFrame;

namespace ExpressThat.LovelyGit.Services.NativeMessaging;

internal sealed partial class NativeMessaging
{
    private const string PostCommand = "Post";
    private const int EnvelopeVersion = 2;

    public void Send<TBody>(
        NativeMessageType messageType,
        NativeMessageResponse<TBody> response,
        JsonTypeInfo<NativeMessageResponse<TBody>> jsonTypeInfo)
    {
        SendPost(messageType.ToMessageId(), JsonSerializer.SerializeToElement(response, jsonTypeInfo));
    }

    public void Send<TBody>(
        NativeMessageType messageType,
        TBody body,
        JsonTypeInfo<NativeMessageResponse<TBody>> jsonTypeInfo)
    {
        Send(
            messageType,
            new NativeMessageResponse<TBody>(string.Empty, true, body, null, null),
            jsonTypeInfo);
    }

    private void SendPost(string id, string data)
    {
        SendPost(id, CreateJsonElement(data));
    }

    private void SendPost(string id, JsonElement? data)
    {
        using var trace = LovelyGitTrace.Time(
            "native.send-post",
            $"{id} data={data?.ValueKind.ToString() ?? "null"}");
        var activeWindow = GetActiveWindow();
        if (activeWindow is null)
        {
            return;
        }

        var envelope = new NativeMessageEnvelope(
            id,
            PostCommand,
            data,
            EnvelopeVersion,
            null,
            null);

        string message;
        using (LovelyGitTrace.Time("native.serialize-envelope", id))
        {
            message = JsonSerializer.Serialize(
                envelope,
                NativeMessagingJsonContext.Default.NativeMessageEnvelope);
        }

        SendWebMessage(activeWindow, message);
    }

    private static JsonElement CreateJsonElement(string value)
    {
        return JsonSerializer.SerializeToElement(
            value,
            NativeMessagingJsonContext.Default.String);
    }

    private static void SendWebMessage(IInfiniFrameWindow activeWindow, string message)
    {
        using var trace = LovelyGitTrace.Time(
            "native.send-web-message",
            $"chars={message.Length}");
        if (activeWindow.IsClosed)
        {
            return;
        }

        if (activeWindow.ManagedThreadId == Environment.CurrentManagedThreadId)
        {
            activeWindow.SendWebMessage(message);
            return;
        }

        activeWindow.Invoke(() =>
        {
            if (!activeWindow.IsClosed)
            {
                activeWindow.SendWebMessage(message);
            }
        });
    }
}
