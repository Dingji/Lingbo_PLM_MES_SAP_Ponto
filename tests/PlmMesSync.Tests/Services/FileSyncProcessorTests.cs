using System.Reflection;
using FileSyncService.Services;

namespace PlmMesSync.Tests.Services;

public class FileSyncProcessorTests
{
    private static MethodInfo? GetSanitizeMethod(string name)
    {
        return typeof(FileSyncProcessor)
            .GetMethod(name, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
    }

    [Fact]
    public void SanitizePath_NormalInput_ReturnsSame()
    {
        var method = GetSanitizeMethod("SanitizePath");
        Assert.NotNull(method);

        var result = method.Invoke(null, new object[] { "NormalFolder" });
        Assert.Equal("NormalFolder", result);
    }

    [Fact]
    public void SanitizePath_DoubleDots_Removed()
    {
        var method = GetSanitizeMethod("SanitizePath");
        Assert.NotNull(method);

        var result = method.Invoke(null, new object[] { ".." });
        Assert.Equal("_default", result);
    }

    [Fact]
    public void SanitizePath_PathTraversal_Blocked()
    {
        var method = GetSanitizeMethod("SanitizePath");
        Assert.NotNull(method);

        var result = method.Invoke(null, new object[] { "../../etc/passwd" });
        Assert.DoesNotContain("..", result?.ToString() ?? "");
        Assert.DoesNotContain("/", result?.ToString() ?? "");
        Assert.DoesNotContain("\\", result?.ToString() ?? "");
    }

    [Fact]
    public void SanitizePath_Dotslash_Removed()
    {
        var method = GetSanitizeMethod("SanitizePath");
        Assert.NotNull(method);

        // "....//" after removing ".." becomes "..//" which still has ".."
        var result = method.Invoke(null, new object[] { "....//" });
        Assert.DoesNotContain("..", result?.ToString() ?? "");
    }

    [Fact]
    public void SanitizePath_PathWithInvalidChars_Sanitized()
    {
        var method = GetSanitizeMethod("SanitizePath");
        Assert.NotNull(method);

        var invalid = string.Join("", Path.GetInvalidPathChars());
        var result = method.Invoke(null, new object[] { $"folder{invalid}name" });
        Assert.DoesNotContain(invalid, result?.ToString() ?? "");
    }

    [Fact]
    public void SanitizeFileName_NormalInput_ReturnsSame()
    {
        var method = GetSanitizeMethod("SanitizeFileName");
        Assert.NotNull(method);

        var result = method.Invoke(null, new object[] { "normal_file.txt" });
        Assert.Equal("normal_file.txt", result);
    }

    [Fact]
    public void SanitizeFileName_PathTraversal_Blocked()
    {
        var method = GetSanitizeMethod("SanitizeFileName");
        Assert.NotNull(method);

        var result = method.Invoke(null, new object[] { "../../malicious.txt" });
        Assert.DoesNotContain("..", result?.ToString() ?? "");
        Assert.DoesNotContain("/", result?.ToString() ?? "");
    }

    [Fact]
    public void SanitizeFileName_Empty_ReturnsDefault()
    {
        var method = GetSanitizeMethod("SanitizeFileName");
        Assert.NotNull(method);

        var result = method.Invoke(null, new object[] { "" });
        Assert.Equal("unnamed_file", result);
    }

    [Fact]
    public void SanitizePath_Empty_ReturnsDefault()
    {
        var method = GetSanitizeMethod("SanitizePath");
        Assert.NotNull(method);

        var result = method.Invoke(null, new object[] { "" });
        Assert.Equal("_default", result);
    }

    [Fact]
    public void SanitizeFileName_InvalidChars_Removed()
    {
        var method = GetSanitizeMethod("SanitizeFileName");
        Assert.NotNull(method);

        var invalid = string.Join("", Path.GetInvalidFileNameChars());
        var result = method.Invoke(null, new object[] { $"file{invalid}.txt" });
        Assert.DoesNotContain(invalid, result?.ToString() ?? "");
        Assert.Contains("_", result?.ToString() ?? "");
    }
}
