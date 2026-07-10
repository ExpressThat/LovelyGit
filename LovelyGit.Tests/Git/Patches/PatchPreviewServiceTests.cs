using ExpressThat.LovelyGit.Services.Git.Patches;

namespace LovelyGit.Tests.Git.Patches;

public sealed class PatchPreviewServiceTests
{
    [Fact]
    public async Task ReadAsync_StreamsFileSummariesAndLineCounts()
    {
        var path = Path.GetTempFileName();
        try
        {
            await File.WriteAllTextAsync(path, PatchText);

            var preview = await PatchPreviewService.ReadAsync(path, CancellationToken.None);

            Assert.True(preview.Selected);
            Assert.Equal(Path.GetFullPath(path), preview.Path);
            Assert.Equal(2, preview.Files.Count);
            Assert.Collection(
                preview.Files,
                file =>
                {
                    Assert.Equal("first file.txt", file.Path);
                    Assert.Equal(1, file.Additions);
                    Assert.Equal(1, file.Deletions);
                },
                file =>
                {
                    Assert.Equal("new.txt", file.Path);
                    Assert.Equal(2, file.Additions);
                    Assert.Equal(0, file.Deletions);
                });
            Assert.Equal(3, preview.TotalAdditions);
            Assert.Equal(1, preview.TotalDeletions);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task ReadAsync_RejectsFilesWithoutUnifiedDiffHeaders()
    {
        var path = Path.GetTempFileName();
        try
        {
            await File.WriteAllTextAsync(path, "not a patch\n");

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => PatchPreviewService.ReadAsync(path, CancellationToken.None));

            Assert.Contains("unified Git patch", exception.Message);
        }
        finally
        {
            File.Delete(path);
        }
    }

    private const string PatchText = """
        diff --git a/first file.txt b/first file.txt
        --- a/first file.txt
        +++ b/first file.txt
        @@ -1 +1 @@
        -old
        +new
        diff --git a/new.txt b/new.txt
        --- /dev/null
        +++ b/new.txt
        @@ -0,0 +1,2 @@
        +one
        +two
        """;
}
