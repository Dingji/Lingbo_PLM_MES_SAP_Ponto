using MdmSyncService.Models;

namespace MdmSyncService.Persistence;

/// <summary>
/// Abstraction over the persistent cache store for failed MDM batches.
/// Implementations must be thread-safe (multiple producers, single retry consumer).
/// </summary>
public interface ICacheStore
{
    /// <summary>
    /// Persists a batch of failed items into the cache with a new version stamp.
    /// If an item with the same material_number already exists, it is replaced
    /// (only the latest version is retained).
    /// </summary>
    Task PersistBatchAsync(IReadOnlyList<MdmItem> items, string errorMessage, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves items whose next_retry_at has elapsed, deduplicated to the latest
    /// version per material_number. Returns at most <paramref name="maxCount"/> items.
    /// </summary>
    Task<IReadOnlyList<CachedMdmItem>> GetDueItemsAsync(int maxCount, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes successfully sent items from the cache.
    /// </summary>
    Task MarkSentAsync(IReadOnlyList<long> ids, CancellationToken cancellationToken = default);

    /// <summary>
    /// Increments the attempt count and schedules the next retry with exponential backoff.
    /// </summary>
    Task MarkFailedAsync(IReadOnlyList<long> ids, string errorMessage, int baseBackoffSeconds, int maxBackoffHours, CancellationToken cancellationToken = default);

    /// <summary>
    /// Moves items that exceeded the maximum attempt count to the dead-letter table.
    /// </summary>
    Task MoveToDeadLetterAsync(IReadOnlyList<long> ids, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the number of items currently in the retry cache.
    /// </summary>
    Task<long> GetCacheDepthAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Flushes any pending WAL data to the main database file. Called during graceful shutdown.
    /// </summary>
    Task CheckpointAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns summary information about the dead-letter table so operators can
    /// detect and act on permanently-failed items (e.g. manual re-queue or alert).
    /// </summary>
    Task<DeadLetterInfo> GetDeadLetterInfoAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Lightweight snapshot of the dead-letter table state, used for monitoring and alerting.
/// </summary>
public sealed class DeadLetterInfo
{
    /// <summary>Total number of items in the dead-letter table.</summary>
    public long Count { get; init; }

    /// <summary>Timestamp of the oldest dead-letter entry, or null if empty.</summary>
    public string? OldestDeadAt { get; init; }

    /// <summary>Timestamp of the newest dead-letter entry, or null if empty.</summary>
    public string? NewestDeadAt { get; init; }
}
