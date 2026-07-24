using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

using PlmMesSync.Models.Dto;

namespace PlmMesSync.Services;

public class TriggerStore
{
    private readonly ConcurrentQueue<TriggerRecord> _records = new();
    private int _sequence;
    private const int MaxRecords = 2000;

    public TriggerRecord Add(int bomId, string action, string table)
    {
        var record = new TriggerRecord(
            Interlocked.Increment(ref _sequence),
            bomId, action, table, DateTime.Now);

        _records.Enqueue(record);

        while (_records.Count > MaxRecords)
            _records.TryDequeue(out _);

        return record;
    }

    public IReadOnlyList<TriggerRecord> GetRecent(int count = 200)
    {
        return _records.Reverse().Take(count).ToList();
    }

    public int TotalCount => _sequence;
}
