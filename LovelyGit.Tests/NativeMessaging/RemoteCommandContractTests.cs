using ExpressThat.LovelyGit.Services.NativeMessaging;

namespace LovelyGit.Tests.NativeMessaging;

public sealed class RemoteCommandContractTests
{
    public static TheoryData<NativeMessageType> AwaitedRemoteCommands => new()
    {
        NativeMessageType.FetchRepository,
        NativeMessageType.PullRepository,
        NativeMessageType.PushRepository,
    };

    [Theory]
    [MemberData(nameof(AwaitedRemoteCommands))]
    public void RemoteCommandsHaveCompletionResponses(NativeMessageType command)
    {
        Assert.True(command.HasResponse());
    }
}
