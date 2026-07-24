namespace FileSyncService.Models.Dto;

/// <summary>
/// Represents a recorded request for the dashboard/kanban view.
/// </summary>
public sealed class RequestRecord
{
    /// <summary>
    /// Unique identifier for the request.
    /// </summary>
    public string RequestId { get; set; } = string.Empty;

    /// <summary>
    /// The time the request was received (UTC).
    /// </summary>
    public DateTime ReceivedTimeUtc { get; set; }

    /// <summary>
    /// The time processing completed (UTC). Null if still processing.
    /// </summary>
    public DateTime? CompletedTimeUtc { get; set; }

    /// <summary>
    /// Processing duration in milliseconds. Null if still processing.
    /// </summary>
    public long? DurationMs { get; set; }

    /// <summary>
    /// Current status: "Processing", "Success", "PartialSuccess", "Failed".
    /// </summary>
    public string Status { get; set; } = "Processing";

    /// <summary>
    /// Total number of files in the request.
    /// </summary>
    public int TotalFiles { get; set; }

    /// <summary>
    /// Number of files processed successfully.
    /// </summary>
    public int SuccessCount { get; set; }

    /// <summary>
    /// Number of files that failed.
    /// </summary>
    public int FailedCount { get; set; }

    /// <summary>
    /// The client IP address that made the request.
    /// </summary>
    public string ClientIp { get; set; } = string.Empty;

    /// <summary>
    /// Detailed file results (populated after completion).
    /// </summary>
    public List<FileInfoResult> Results { get; set; } = [];

    /// <summary>
    /// Top-level error message if applicable.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Path to the per-request detailed log file.
    /// </summary>
    public string? LogFile { get; set; }
}
