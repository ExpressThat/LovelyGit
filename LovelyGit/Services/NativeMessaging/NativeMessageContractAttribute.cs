namespace ExpressThat.LovelyGit.Services.NativeMessaging;

[AttributeUsage(AttributeTargets.Field)]
public sealed class NativeMessageContractAttribute : Attribute
{
    public NativeMessageContractAttribute()
    {
    }

    public NativeMessageContractAttribute(Type requestType)
    {
        RequestType = requestType;
    }

    public NativeMessageContractAttribute(Type requestType, Type responseType)
    {
        RequestType = requestType;
        ResponseType = responseType;
    }

    public Type? RequestType { get; set; }

    public Type? ResponseType { get; set; }
}
