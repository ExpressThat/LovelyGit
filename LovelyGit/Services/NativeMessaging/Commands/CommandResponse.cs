using ExpressThat.LovelyGit.Services.TypeGeneration;
using ExpressThat.LovelyGit.Services.NativeMessaging;

namespace ExpressThat.LovelyGit.Services.NativeMessaging.Commands
{
    public interface ICommandResponseWithResult
    {
        object? ResultObject { get; }
    }

    [TypeSharp]
    public class CommandResponseBase
    {
        public string? CommandUniqueId { get; set; }
        public NativeMessageType CommandType { get; set; }
        public bool IsSuccess { get; set; }
        public string? ErrorMessage { get; set; }
    }


    [TypeSharp]
    public class CommandResponse<T> : CommandResponseBase, ICommandResponseWithResult
    {
        public T? Result { get; set; }

        public object? ResultObject => Result;
    }
}
