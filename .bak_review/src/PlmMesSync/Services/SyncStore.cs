using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

using PlmMesSync.Models.Dto;

namespace PlmMesSync.Services;

public class SyncStore
{
    private readonly ConcurrentQueue<SyncRecord> _records = new();
    private int _sequence;
    private const int MaxRecords = 2000;

    public void Add(int bomId, int? itemId, string? itemNumber,
        DateTime triggeredAt, DateTime executedAt, string status,
        string? logFile = null, string? error = null, string? mesStatus = null,
        string table = "BOM")
    {
        var record = new SyncRecord(
            Interlocked.Increment(ref _sequence),
            bomId, itemId, itemNumber,
            triggeredAt, executedAt, status, logFile, error, mesStatus, table);

        _records.Enqueue(record);

        while (_records.Count > MaxRecords)
            _records.TryDequeue(out _);
    }

    public IReadOnlyList<SyncRecord> GetRecent(int count = 200)
    {
        return _records.Reverse().Take(count).ToList();
    }

    public int TotalCount => _sequence;
}
