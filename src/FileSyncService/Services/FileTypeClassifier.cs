namespace FileSyncService.Services;

/// <summary>
/// Determines the MES <c>file_type</c> for a file and whether the file should be transmitted.
/// </summary>
public static class FileTypeClassifier
{
    private const string PickPlacePrefix = "Pick Place for";
    private const string PdfExtension = ".pdf";

    /// <summary>
    /// File code prefixes that classify a file as type "2".
    /// </summary>
    public static readonly string[] Type2FileCodePrefixes =
        ["14100-", "31120-", "31126-", "39110-", "16352-", "14180-", "10000-", "38350-", "38360-", "38380-"];

    /// <summary>
    /// Resolves the MES file_type:
    /// "3" when the file name starts with "Pick Place for";
    /// otherwise "2" when the file code starts with a known prefix;
    /// otherwise "1".
    /// </summary>
    public static string DetermineFileType(string? fileName, string? fileCode)
    {
        if (!string.IsNullOrEmpty(fileName) &&
            fileName.StartsWith(PickPlacePrefix, StringComparison.OrdinalIgnoreCase))
            return "3";

        if (!string.IsNullOrEmpty(fileCode) &&
            Type2FileCodePrefixes.Any(p => fileCode.StartsWith(p, StringComparison.OrdinalIgnoreCase)))
            return "2";

        return "1";
    }

    /// <summary>
    /// Type "3" files are always transmitted. Type "1"/"2" files are transmitted only when they are PDFs.
    /// </summary>
    public static bool ShouldTransmit(string? fileName, string fileType)
    {
        if (fileType == "3")
            return true;

        return !string.IsNullOrEmpty(fileName) &&
               fileName.EndsWith(PdfExtension, StringComparison.OrdinalIgnoreCase);
    }
}
