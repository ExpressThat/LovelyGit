using ExpressThat.LovelyGit.Services.Git.Cli;
using ExpressThat.LovelyGit.Services.Git.Lfs;

namespace LovelyGit.Tests.Git.Lfs;

[Collection(GitShellIntegrationCollection.Name)]
public sealed class GitLfsZeroExitTests
{
    [Fact]
    public async Task Track_ZeroExitWithoutAttributeMutationIsRejected()
    {
        if (!OperatingSystem.IsWindows()) return;

        var directory = Directory.CreateTempSubdirectory("lovelygit-lfs-zero-exit-");
        var attributesPath = Path.Combine(directory.FullName, ".gitattributes");
        try
        {
            var git = new GitCliService();
            await git.ExecuteBufferedAsync(["init"], directory.FullName);
            await File.WriteAllTextAsync(
                attributesPath,
                "existing/** filter=lfs diff=lfs merge=lfs -text\n");
            File.SetAttributes(attributesPath, FileAttributes.ReadOnly);
            var reader = new NativeGitLfsStateReader(git);
            var service = new GitLfsCommandService(git, reader);

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.ExecuteAsync(
                    directory.FullName,
                    GitLfsAction.Track,
                    "missing/**",
                    CancellationToken.None));

            Assert.Contains("did not add", exception.Message);
            var state = await reader.ReadAsync(directory.FullName, CancellationToken.None);
            Assert.Equal(["existing/**"], state.TrackedPatterns);
        }
        finally
        {
            if (File.Exists(attributesPath))
            {
                File.SetAttributes(attributesPath, FileAttributes.Normal);
            }
            directory.Delete(recursive: true);
        }
    }
}
