namespace FileSyncService.Models.Dto;

/// <summary>
/// The unified response envelope for the /fileSync endpoint.
/// </summary>
public sealed class FileSyncResponse
{
    /// <summary>
    /// A unique identifier for this request batch.
    /// </summary>
    public string RequestId { get; set; } = string.Empty;

    /// <summary>
    /// The timestamp when the request was received (UTC).
    /// </summary>
    public DateTime ReceivedTimeUtc { get; set; }

    /// <summary>
    /// The timestamp when processing completed (UTC).
    /// </summary>
    public DateTime CompletedTimeUtc { get; set; }

    /// <summary>
    /// Total processing duration in milliseconds.
    /// </summary>
    public long DurationMs { get; set; }

    /// <summary>
    /// Overall status: "Success", "PartialSuccess", or "Failed".
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Total number of files in the request.
    /// </summary>
    public int TotalFiles { get; set; }

    /// <summary>
    /// Number of files processed successfully.
    /// </summary>
    public int SuccessCount { get; set; }

    /// <summary>
    /// Number of files that failed processing.
    /// </summary>
    public int FailedCount { get; set; }

    /// <summary>
    /// Detailed results for each file.
    /// </summary>
    public List<FileInfoResult> Results { get; set; } = [];

    /// <summary>
    /// Top-level error message if the entire request failed (e.g., invalid JSON).
    /// </summary>
    public string? ErrorMessage { get; set; }
}
