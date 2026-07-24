namespace FileSyncService.Models.Dto;

/// <summary>
/// Represents a single file entry in the sync request.
/// </summary>
public sealed class FileSyncItem
{
    /// <summary>
    /// The inventory code associated with the file.
    /// </summary>
    public string InventroyCode { get; set; } = string.Empty;

    /// <summary>
    /// The unique file code identifier.
    /// </summary>
    public string FileCode { get; set; } = string.Empty;

    /// <summary>
    /// The name of the file.
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// Optional remark or description for the file.
    /// </summary>
    public string Remark { get; set; } = string.Empty;

    /// <summary>
    /// The URL from which the file will be downloaded.
    /// </summary>
    public string FileUrl { get; set; } = string.Empty;

    /// <summary>
    /// Optional relative path under the root directory for local file processing.
    /// When provided, the file is read from RootDirectory/RelativePath instead of downloading.
    /// </summary>
    public string RelativePath { get; set; } = string.Empty;
}

/// <summary>
/// The top-level request body for the /fileSync endpoint.
/// </summary>
public sealed class FileSyncRequest
{
    /// <summary>
    /// The collection of files to download and analyze.
    /// </summary>
    public List<FileSyncItem> Files { get; set; } = [];
}
