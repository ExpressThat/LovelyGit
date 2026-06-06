namespace ExpressThat.LovelyGit.Services.Git.LovelyFastGitParser.Packs;

internal static class GitPackFileHelpers
{
    public static async Task ReadExactlyAsync(Stream stream, byte[] buffer, CancellationToken cancellationToken)
    {
        var readTotal = 0;
        while (readTotal < buffer.Length)
        {
            var read = await stream.ReadAsync(buffer.AsMemory(readTotal), cancellationToken).ConfigureAwait(false);
            if (read == 0)
            {
                throw new EndOfStreamException();
            }

            readTotal += read;
        }
    }
}
