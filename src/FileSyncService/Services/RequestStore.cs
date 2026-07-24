using FileSyncService.Models.Dto;

using System.Collections.Concurrent;
using System.IO;
using System.Text.Json;

namespace FileSyncService.Services;

public sealed class RequestStore
{
    private readonly ConcurrentDictionary<string, RequestRecord> _records = new();
    private readonly ConcurrentQueue<string> _orderedIds = new();
    private const int MaxRecords = 500;
    private readonly string _persistPath;
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented = false,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public RequestStore() : this(Path.Combine(AppContext.BaseDirectory, "log", ".request_store.json"))
    {
    }

    public RequestStore(string? persistPath)
    {
        _persistPath = persistPath ?? string.Empty;
        if (!string.IsNullOrEmpty(_persistPath))
            LoadFromFile();
    }

    public void Add(RequestRecord record)
    {
        _records[record.RequestId] = record;
        _orderedIds.Enqueue(record.RequestId);

        while (_orderedIds.Count > MaxRecords)
        {
            if (_orderedIds.TryDequeue(out var oldestId))
            {
                _records.TryRemove(oldestId, out _);
            }
        }

        SaveToFile();
    }

    public void Update(RequestRecord record)
    {
        _records[record.RequestId] = record;
        SaveToFile();
    }

    public List<RequestRecord> GetRecent(int count = 100)
    {
        return _records.Values
            .OrderByDescending(r => r.ReceivedTimeUtc)
            .Take(count)
            .ToList();
    }

    public RequestRecord? GetById(string requestId)
    {
        _records.TryGetValue(requestId, out var record);
        return record;
    }

    public (int Total, int Success, int Failed, int Processing) GetStats()
    {
        var all = _records.Values.ToList();
        var success = all.Count(r => r.Status == "Success");
        var failed = all.Count(r => r.Status == "Failed");
        var processing = all.Count(r => r.Status == "Processing");
        return (all.Count, success, failed, processing);
    }

    private void LoadFromFile()
    {
        try
        {
            if (!File.Exists(_persistPath)) return;

            var json = File.ReadAllText(_persistPath);
            var data = JsonSerializer.Deserialize<List<RequestRecord>>(json, JsonOpts);
            if (data is null) return;

            foreach (var r in data)
            {
                _records[r.RequestId] = r;
                _orderedIds.Enqueue(r.RequestId);
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
        try
        {
            var dir = Path.GetDirectoryName(_persistPath);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

            var json = JsonSerializer.Serialize(_records.Values.ToList(), JsonOpts);
            File.WriteAllText(_persistPath, json);
        }
        catch
        {
            // Silently fail rather than disrupt request processing
        }
    }
}
