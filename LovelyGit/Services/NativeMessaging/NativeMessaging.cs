using System.Text.Json;
using System.Diagnostics;
using ExpressThat.LovelyGit.Services.Diagnostics;
using ExpressThat.LovelyGit.Services.NativeMessaging.Commands;
using InfiniFrame;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.Options;

namespace ExpressThat.LovelyGit.Services.NativeMessaging;

internal sealed partial class NativeMessaging : INativeMessaging
{
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
        using var trace = LovelyGitTrace.Time(
            "native.handle",
            $"{messageType} payload={NativeMessageMetricsFactory.CountUtf8Bytes(payload)}");
        var startedAt = Stopwatch.GetTimestamp();
        var requestPayloadBytes = NativeMessageMetricsFactory.CountUtf8Bytes(payload);
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
                "Native message payload was empty or invalid.",
                NativeMessageMetricsFactory.Create(startedAt, requestPayloadBytes));
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
                commandResponse.ErrorMessage,
                NativeMessageMetricsFactory.Create(startedAt, requestPayloadBytes));
        }
        catch (Exception exception)
        {
            if (!messageType.HasResponse())
            {
                return null;
            }

            return CreateResponse(
                request.MessageId,
                false,
                null,
                exception.Message,
                NativeMessageMetricsFactory.Create(startedAt, requestPayloadBytes));
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

    private IInfiniFrameWindow? GetActiveWindow()
    {
        lock (syncRoot)
        {
            return window is { IsClosed: false } ? window : null;
        }
    }
}
