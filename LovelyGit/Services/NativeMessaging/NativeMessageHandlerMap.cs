using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace ExpressThat.LovelyGit.Services.NativeMessaging;

internal sealed class NativeMessageHandlerMap
{
    private readonly Dictionary<NativeMessageType, INativeMessageHandler> handlers = [];

    public NativeMessageHandlerMap Map<TRequest, TResponse>(
        NativeMessageType messageType,
        JsonTypeInfo<NativeMessageRequest<TRequest>> requestJsonTypeInfo,
        JsonTypeInfo<NativeMessageResponse<TResponse>> responseJsonTypeInfo,
        Func<IServiceProvider, TRequest, TResponse> handler)
    {
        handlers[messageType] = new NativeMessageHandler<TRequest, TResponse>(
            requestJsonTypeInfo,
            responseJsonTypeInfo,
            handler);

        return this;
    }

    public NativeMessageHandlerMap Map<TRequest>(
        NativeMessageType messageType,
        JsonTypeInfo<NativeMessageRequest<TRequest>> requestJsonTypeInfo,
        Action<IServiceProvider, TRequest> handler)
    {
        handlers[messageType] = new NativeMessageHandler<TRequest>(
            requestJsonTypeInfo,
            handler);

        return this;
    }

    public bool TryHandle(
        NativeMessageType messageType,
        IServiceProvider services,
        string? payload,
        out JsonElement? response)
    {
        if (!handlers.TryGetValue(messageType, out var handler))
        {
            response = JsonSerializer.SerializeToElement(
                new NativeMessageResponse<object>(
                    string.Empty,
                    false,
                    null,
                    $"No native message handler is registered for '{messageType}'.",
                    null),
                NativeMessagingJsonContext.Default.NativeMessageResponseObject);

            return false;
        }

        response = handler.Handle(services, payload);
        return true;
    }

    private interface INativeMessageHandler
    {
        JsonElement? Handle(IServiceProvider services, string? payload);
    }

    private sealed class NativeMessageHandler<TRequest, TResponse>(
        JsonTypeInfo<NativeMessageRequest<TRequest>> requestJsonTypeInfo,
        JsonTypeInfo<NativeMessageResponse<TResponse>> responseJsonTypeInfo,
        Func<IServiceProvider, TRequest, TResponse> handler) : INativeMessageHandler
    {
        public JsonElement? Handle(IServiceProvider services, string? payload)
        {
            var request = DeserializeRequest(payload);
            if (request is null)
            {
                return CreateErrorResponse(string.Empty, "Native message payload was empty or invalid.");
            }

            try
            {
                var body = handler(services, request.Body);
                return JsonSerializer.SerializeToElement(
                    new NativeMessageResponse<TResponse>(request.MessageId, true, body, null, null),
                    responseJsonTypeInfo);
            }
            catch (Exception exception)
            {
                return CreateErrorResponse(request.MessageId, exception.Message);
            }
        }

        private NativeMessageRequest<TRequest>? DeserializeRequest(string? payload)
        {
            if (string.IsNullOrWhiteSpace(payload))
            {
                return null;
            }

            try
            {
                return JsonSerializer.Deserialize(payload, requestJsonTypeInfo);
            }
            catch (JsonException)
            {
                return null;
            }
        }

        private JsonElement CreateErrorResponse(string messageId, string error)
        {
            return JsonSerializer.SerializeToElement(
                new NativeMessageResponse<TResponse>(messageId, false, default, error, null),
                responseJsonTypeInfo);
        }
    }

    private sealed class NativeMessageHandler<TRequest>(
        JsonTypeInfo<NativeMessageRequest<TRequest>> requestJsonTypeInfo,
        Action<IServiceProvider, TRequest> handler) : INativeMessageHandler
    {
        public JsonElement? Handle(IServiceProvider services, string? payload)
        {
            var request = DeserializeRequest(payload);
            if (request is null)
            {
                return null;
            }

            try
            {
                handler(services, request.Body);
            }
            catch (Exception exception)
            {
                System.Diagnostics.Trace.TraceWarning(
                    "Native message handler failed without a response contract: {0}",
                    exception);
            }

            return null;
        }

        private NativeMessageRequest<TRequest>? DeserializeRequest(string? payload)
        {
            if (string.IsNullOrWhiteSpace(payload))
            {
                return null;
            }

            try
            {
                return JsonSerializer.Deserialize(payload, requestJsonTypeInfo);
            }
            catch (JsonException)
            {
                return null;
            }
        }
    }
}
