using System.Text.Json;
using ExpressThat.LovelyGit.Services.NativeMessaging.Commands;

namespace ExpressThat.LovelyGit.Services.NativeMessaging;

internal sealed partial class NativeMessaging
{
    private static JsonElement CreateResponse(
        string messageId,
        bool success,
        JsonElement? body,
        string? error,
        NativeMessageMetrics? metrics)
    {
        return JsonSerializer.SerializeToElement(
            new NativeMessageResponse<JsonElement?>(
                messageId,
                success,
                body,
                error,
                metrics),
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
}
