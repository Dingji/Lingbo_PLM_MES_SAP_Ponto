using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;

using PlmMesSync.Models.Dto;

namespace PlmMesSync.Services;

public class TriggerStore
{
    private readonly ConcurrentQueue<TriggerRecord> _records = new();
    private int _sequence;
    private const int MaxRecords = 2000;
    private readonly string _persistPath;
    private readonly SemaphoreSlim _fileLock = new(1, 1);
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented = false,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public TriggerStore() : this(Path.Combine(AppContext.BaseDirectory, "log", ".trigger_store.json"))
    {
    }

    public TriggerStore(string? persistPath)
    {
        _persistPath = persistPath ?? string.Empty;
        if (!string.IsNullOrEmpty(_persistPath))
            LoadFromFile();
    }

    public TriggerRecord Add(int bomId, string action, string table)
    {
        var record = new TriggerRecord(
            Interlocked.Increment(ref _sequence),
            bomId, action, table, DateTime.Now);

        _records.Enqueue(record);

        while (_records.Count > MaxRecords)
            _records.TryDequeue(out _);

        SaveToFile();
        return record;
    }

    public IReadOnlyList<TriggerRecord> GetRecent(int count = 200)
    {
        return _records.Reverse().Take(count).ToList();
    }

    public int TotalCount => _sequence;

    private void LoadFromFile()
    {
        try
        {
            if (!File.Exists(_persistPath)) return;

            var json = File.ReadAllText(_persistPath);
            var data = JsonSerializer.Deserialize<TriggerStoreData>(json, JsonOpts);
            if (data is null) return;

            _sequence = data.Sequence;
            if (data.Records is not null)
            {
                foreach (var r in data.Records)
                    _records.Enqueue(r);
            }
        }
        catch
        {
            // Ignore deserialization errors — start fresh
        }
    }

    private void SaveToFile()
    {
        if (string.IsNullOrEmpty(_persistPath)) return;
        _fileLock.Wait();
        try
        {
            var dir = Path.GetDirectoryName(_persistPath);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

            var data = new TriggerStoreData
            {
                Sequence = _sequence,
                Records = _records.ToList()
            };

            var json = JsonSerializer.Serialize(data, JsonOpts);
            var tmpPath = _persistPath + ".tmp";
            File.WriteAllText(tmpPath, json);
            File.Move(tmpPath, _persistPath, overwrite: true);
        }
        catch
        {
            // Silently fail rather than disrupt sync operations
        }
        finally
        {
            _fileLock.Release();
        }
    }

    private class TriggerStoreData
    {
        public int Sequence { get; set; }
        public List<TriggerRecord> Records { get; set; } = new();
    }
}
