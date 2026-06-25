using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using ExpressThat.LovelyGit.Services.NativeMessaging.Commands;
using InfiniFrame;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.Options;

namespace ExpressThat.LovelyGit.Services.NativeMessaging;

internal sealed class NativeMessaging : INativeMessaging
{
    private const string PostCommand = "Post";
    private const int EnvelopeVersion = 2;

    private readonly CommandResolver commandResolver;
    private readonly JsonSerializerOptions serializerOptions;
    private readonly object syncRoot = new();
    private IInfiniFrameWindow? window;

    public NativeMessaging(
        CommandResolver commandResolver,
        IOptions<JsonOptions> jsonOptions)
    {
        this.commandResolver = commandResolver;
        serializerOptions = jsonOptions.Value.SerializerOptions;
    }

    public bool HasWindow
    {
        get
        {
            lock (syncRoot)
            {
                return window is { IsClosed: false };
            }
        }
    }

    public void AttachWindow(IInfiniFrameWindow activeWindow)
    {
        lock (syncRoot)
        {
            window = activeWindow;
        }
    }

    public void Handle(NativeMessageType messageType, IServiceProvider services, string? payload)
    {
        _ = Task.Run(async () =>
        {
            var response = await HandleCommand(messageType, payload);
            if (response is not null)
            {
                SendPost(messageType.ToMessageId(), response.Value);
            }
        });
    }

    private async Task<JsonElement?> HandleCommand(
        NativeMessageType messageType,
        string? payload)
    {
        var request = DeserializeRequest(payload);
        if (request is null)
        {
            if (!messageType.HasResponse())
            {
                return null;
            }

            return CreateResponse(
                string.Empty,
                false,
                null,
                "Native message payload was empty or invalid.");
        }

        try
        {
            var commandResponse = await commandResolver.ResolveCommand(new NativeCommand<JsonElement>
            {
                CommandUniqueId = request.MessageId,
                CommandType = messageType,
                Arguments = request.Body,
            });

            if (!messageType.HasResponse())
            {
                return null;
            }

            return CreateResponse(
                request.MessageId,
                commandResponse.IsSuccess,
                ExtractResult(commandResponse),
                commandResponse.ErrorMessage);
        }
        catch (Exception exception)
        {
            if (!messageType.HasResponse())
            {
                return null;
            }

            return CreateResponse(request.MessageId, false, null, exception.Message);
        }
    }

    private static NativeMessageRequest<JsonElement>? DeserializeRequest(string? payload)
    {
        if (string.IsNullOrWhiteSpace(payload))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize(
                payload,
                NativeMessagingJsonContext.Default.NativeMessageRequestJsonElement);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static JsonElement CreateResponse(
        string messageId,
        bool success,
        JsonElement? body,
        string? error)
    {
        return JsonSerializer.SerializeToElement(
            new NativeMessageResponse<JsonElement?>(messageId, success, body, error),
            NativeMessagingJsonContext.Default.NativeMessageResponseJsonElement);
    }

    private JsonElement? ExtractResult(CommandResponseBase response)
    {
        if (response is not ICommandResponseWithResult responseWithResult)
        {
            return null;
        }

        var result = responseWithResult.ResultObject;
        return result is null
            ? null
            : JsonSerializer.SerializeToElement(result, result.GetType(), serializerOptions);
    }

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
            new NativeMessageResponse<TBody>(string.Empty, true, body, null),
            jsonTypeInfo);
    }

    private void SendPost(string id, string data)
    {
        SendPost(id, CreateJsonElement(data));
    }

    private void SendPost(string id, JsonElement? data)
    {
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

        var message = JsonSerializer.Serialize(
            envelope,
            NativeMessagingJsonContext.Default.NativeMessageEnvelope);

        SendWebMessage(activeWindow, message);
    }

    private IInfiniFrameWindow? GetActiveWindow()
    {
        lock (syncRoot)
        {
            return window is { IsClosed: false } ? window : null;
        }
    }

    private static JsonElement CreateJsonElement(string value)
    {
        return JsonSerializer.SerializeToElement(
            value,
            NativeMessagingJsonContext.Default.String);
    }

    private static void SendWebMessage(IInfiniFrameWindow activeWindow, string message)
    {
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
