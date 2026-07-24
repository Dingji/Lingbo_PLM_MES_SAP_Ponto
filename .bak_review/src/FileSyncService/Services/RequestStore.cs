using FileSyncService.Models.Dto;

using System.Collections.Concurrent;

namespace FileSyncService.Services;

/// <summary>
/// Thread-safe in-memory store for request records, used by the dashboard.
/// Maintains a bounded history of recent requests.
/// </summary>
public sealed class RequestStore
{
    private readonly ConcurrentDictionary<string, RequestRecord> _records = new();
    private readonly ConcurrentQueue<string> _orderedIds = new();
    private const int MaxRecords = 500;

    /// <summary>
    /// Adds a new request record to the store.
    /// </summary>
    /// <param name="record">The request record to add.</param>
    public void Add(RequestRecord record)
    {
        _records[record.RequestId] = record;
        _orderedIds.Enqueue(record.RequestId);

        // Evict oldest records when exceeding the maximum capacity.
        while (_orderedIds.Count > MaxRecords)
        {
            if (_orderedIds.TryDequeue(out var oldestId))
            {
                _records.TryRemove(oldestId, out _);
            }
        }
    }

    /// <summary>
    /// Updates an existing request record in the store.
    /// </summary>
    /// <param name="record">The updated request record.</param>
    public void Update(RequestRecord record)
    {
        _records[record.RequestId] = record;
    }

    /// <summary>
    /// Retrieves all records ordered by most recent first.
    /// </summary>
    /// <param name="count">Maximum number of records to return.</param>
    /// <returns>A list of request records, most recent first.</returns>
    public List<RequestRecord> GetRecent(int count = 100)
    {
        return _records.Values
            .OrderByDescending(r => r.ReceivedTimeUtc)
            .Take(count)
            .ToList();
    }

    /// <summary>
    /// Retrieves a single record by its request ID.
    /// </summary>
    /// <param name="requestId">The unique request identifier.</param>
    /// <returns>The request record if found; otherwise null.</returns>
    public RequestRecord? GetById(string requestId)
    {
        _records.TryGetValue(requestId, out var record);
        return record;
    }

    /// <summary>
    /// Gets summary statistics for the dashboard.
    /// </summary>
    public (int Total, int Success, int Failed, int Processing) GetStats()
    {
        var all = _records.Values.ToList();
        var success = all.Count(r => r.Status == "Success");
        var failed = all.Count(r => r.Status == "Failed");
        var processing = all.Count(r => r.Status == "Processing");
        return (all.Count, success, failed, processing);
    }
}
