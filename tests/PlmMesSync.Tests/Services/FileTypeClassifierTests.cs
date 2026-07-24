using FileSyncService.Services;

namespace PlmMesSync.Tests.Services;

public class FileTypeClassifierTests
{
    [Theory]
    [InlineData("Pick Place for Board A.pdf")]
    [InlineData("pick place for Board A.zip")]
    [InlineData("PICK PLACE FOR X.doc")]
    public void DetermineFileType_PickPlacePrefix_Returns3(string fileName)
    {
        Assert.Equal("3", FileTypeClassifier.DetermineFileType(fileName, "99999-1"));
    }

    [Theory]
    [InlineData("14100-1")]
    [InlineData("31120-2")]
    [InlineData("31126-3")]
    [InlineData("39110-4")]
    [InlineData("16352-5")]
    [InlineData("14180-6")]
    [InlineData("10000-7")]
    [InlineData("38350-8")]
    [InlineData("38360-9")]
    [InlineData("38380-0")]
    public void DetermineFileType_Type2Prefix_Returns2(string fileCode)
    {
        Assert.Equal("2", FileTypeClassifier.DetermineFileType("SomeDoc.pdf", fileCode));
    }

    [Theory]
    [InlineData("12345-1")]
    [InlineData("KK70000010")]
    [InlineData("")]
    public void DetermineFileType_OtherCode_Returns1(string fileCode)
    {
        Assert.Equal("1", FileTypeClassifier.DetermineFileType("SomeDoc.pdf", fileCode));
    }

    [Fact]
    public void DetermineFileType_PickPlaceWinsOverType2Prefix()
    {
        Assert.Equal("3", FileTypeClassifier.DetermineFileType("Pick Place for X.pdf", "14100-1"));
    }

    [Theory]
    [InlineData("doc.pdf", "1", true)]
    [InlineData("doc.PDF", "2", true)]
    [InlineData("doc.zip", "1", false)]
    [InlineData("doc.zip", "2", false)]
    [InlineData("doc.zip", "3", true)]
    [InlineData("doc.exe", "3", true)]
    public void ShouldTransmit_MatchesPdfRule(string fileName, string fileType, bool expected)
    {
        Assert.Equal(expected, FileTypeClassifier.ShouldTransmit(fileName, fileType));
    }

    [Theory]
    [InlineData("archive.zip", true)]
    [InlineData("archive.tar.gz", true)]
    [InlineData("archive.7z", true)]
    [InlineData("archive.rar", true)]
    [InlineData("doc.pdf", false)]
    [InlineData("doc", false)]
    public void IsArchiveName_DetectsByExtension(string name, bool expected)
    {
        Assert.Equal(expected, ArchiveExtractor.IsArchiveName(name));
    }
}
