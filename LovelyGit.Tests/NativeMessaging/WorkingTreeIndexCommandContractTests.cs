using ExpressThat.LovelyGit.Services.NativeMessaging;

namespace LovelyGit.Tests.NativeMessaging;

public sealed class WorkingTreeIndexCommandContractTests
{
    public static TheoryData<NativeMessageType> AwaitedIndexCommands => new()
    {
        NativeMessageType.StageWorkingTreeFiles,
        NativeMessageType.UnstageWorkingTreeFiles,
        NativeMessageType.StageWorkingTreeLine,
        NativeMessageType.UnstageWorkingTreeLine,
        NativeMessageType.StageWorkingTreeHunk,
        NativeMessageType.UnstageWorkingTreeHunk,
    };

    [Theory]
    [MemberData(nameof(AwaitedIndexCommands))]
    public void IndexMutationCommandsHaveResponses(NativeMessageType command)
    {
        Assert.True(command.HasResponse());
    }
}
