namespace ExpressThat.LovelyGit.Services.NativeMessaging;

internal static class NativeMessageTypeExtensions
{
    public static string ToMessageId(this NativeMessageType messageType)
    {
        return messageType.ToString();
    }

    public static bool HasResponse(this NativeMessageType messageType)
    {
        var member = typeof(NativeMessageType).GetMember(messageType.ToString())[0];
        var contract = member
            .GetCustomAttributes(typeof(NativeMessageContractAttribute), inherit: false)
            .OfType<NativeMessageContractAttribute>()
            .FirstOrDefault();

        return contract?.ResponseType is not null;
    }
}
