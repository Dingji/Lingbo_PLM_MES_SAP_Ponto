using System.Text.Json.Serialization;

namespace MdmSyncService.Models;

/// <summary>
/// Represents a cached MDM item awaiting retry, including versioning and attempt metadata.
/// Stored in SQLite to survive process restarts.
/// </summary>
public sealed class CachedMdmItem
{
    public long Id { get; init; }

    /// <summary>
    /// Deduplication key — the material's new_sap_number.
    /// Only the highest-version entry per material is sent on retry.
    /// </summary>
    public string MaterialNumber { get; init; } = string.Empty;

    /// <summary>
    /// Monotonically increasing version (DateTime.UtcNow.Ticks at insertion time).
    /// Higher version = newer data for the same material.
    /// </summary>
    public long Version { get; init; }

    /// <summary>
    /// Full MdmItem serialized as JSON for deferred sending.
    /// </summary>
    public string PayloadJson { get; init; } = string.Empty;

    /// <summary>
    /// Number of send attempts already made for this cached entry.
    /// </summary>
    public int AttemptCount { get; init; }

    /// <summary>
    /// Unix epoch seconds (UTC) after which this item becomes eligible for retry.
    /// Stored as an integer so eligibility comparisons are numeric, not lexicographic.
    /// </summary>
    public long NextRetryAt { get; init; }

    public string CreatedAt { get; init; } = string.Empty;

    public string UpdatedAt { get; init; } = string.Empty;

    /// <summary>
    /// Error message from the most recent failed attempt.
    /// </summary>
    public string? LastError { get; init; }

    /// <summary>
    /// Deserialized payload for sending. Populated by the cache store on read.
    /// </summary>
    [JsonIgnore]
    public MdmItem? Item { get; set; }
}
