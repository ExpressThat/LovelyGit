using System.Runtime;

namespace ExpressThat.LovelyGit.Services.Git.WorkingTree;

internal static class GitIndexMemory
{
    private const int LargeIndexBytes = 32 * 1024 * 1024;

    public static void ReleaseLargeBuffer(int length)
    {
        if (length < LargeIndexBytes)
        {
            return;
        }

        GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
        GC.Collect(2, GCCollectionMode.Aggressive, blocking: false, compacting: true);
    }
}
