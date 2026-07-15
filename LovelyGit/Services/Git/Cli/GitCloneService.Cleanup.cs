namespace ExpressThat.LovelyGit.Services.Git.Cli;

internal sealed partial class GitCloneService
{
    private const int CleanupAttemptCount = 6;

    internal static async Task DeletePartialDestinationAsync(
        string destinationPath,
        Exception cloneFailure)
    {
        Exception? cleanupFailure = null;
        for (var attempt = 0; attempt < CleanupAttemptCount; attempt++)
        {
            try
            {
                if (!Directory.Exists(destinationPath))
                {
                    return;
                }

                Directory.Delete(destinationPath, recursive: true);
                return;
            }
            catch (UnauthorizedAccessException exception)
            {
                cleanupFailure = exception;
                TryClearReadOnlyAttributes(destinationPath);
            }
            catch (IOException exception)
            {
                cleanupFailure = exception;
            }

            if (attempt + 1 < CleanupAttemptCount)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(50 * (attempt + 1)))
                    .ConfigureAwait(false);
            }
        }

        throw new InvalidOperationException(
            $"The clone stopped, but its partial destination could not be removed: {destinationPath}",
            new AggregateException(cloneFailure, cleanupFailure!));
    }

    private static void TryClearReadOnlyAttributes(string destinationPath)
    {
        try
        {
            foreach (var file in Directory.EnumerateFiles(
                         destinationPath,
                         "*",
                         SearchOption.AllDirectories))
            {
                var attributes = File.GetAttributes(file);
                if ((attributes & FileAttributes.ReadOnly) != 0)
                {
                    File.SetAttributes(file, attributes & ~FileAttributes.ReadOnly);
                }
            }
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
        }
    }
}
