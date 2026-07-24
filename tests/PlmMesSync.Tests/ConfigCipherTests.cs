using PlmMesSync;

namespace PlmMesSync.Tests;

public class ConfigCipherTests
{
    [Fact]
    public void DecryptConnectionString_NoMatch_ReturnsOriginal()
    {
        var result = ConfigCipher.DecryptConnectionString(
            "Data Source=localhost;User Id=test;Password=plain;");

        Assert.Equal("Data Source=localhost;User Id=test;Password=plain;", result);
    }

    [Fact]
    public void DecryptConnectionString_EmptyString_ReturnsEmpty()
    {
        var result = ConfigCipher.DecryptConnectionString(string.Empty);
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void DecryptConnectionString_NullInput_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            ConfigCipher.DecryptConnectionString(null!));
    }
}
