using ExpressThat.LovelyGit.Services.Dialogs;

namespace LovelyGit.Tests.Dialogs;

public sealed class InfiniFrameSaveFilePickerTests
{
    [Theory]
    [InlineData("patch", "Patch files")]
    [InlineData("ZIP", "Archive files")]
    [InlineData("tar", "Archive files")]
    [InlineData("txt", "Files")]
    public void GetFilterLabel_DescribesTheSelectedFileType(
        string extension,
        string expected)
    {
        Assert.Equal(expected, InfiniFrameSaveFilePicker.GetFilterLabel([extension]));
    }
}
