using System.Text.Json;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Options;
using MdmSyncService.Configuration;
using MdmSyncService.Models;
using MdmSyncService.Serialization;

namespace MdmSyncService.Persistence;

/// <summary>
/// SQLite-backed persistent cache for failed MDM batches.
/// Uses WAL journal mode for concurrent read access and a SemaphoreSlim to serialize writes.
/// Registered as a singleton — thread-safe for multiple producers and the retry consumer.
/// </summary>
public sealed class SqliteCacheStore : ICacheStore, IAsyncDisposable
{
    // Serialization options are shared project-wide via MdmJsonSerializer.

    private readonly string _connectionString;
    private readonly SemaphoreSlim _writeLock = new(1, 1);
    private readonly ILogger<SqliteCacheStore> _logger;

    public SqliteCacheStore(IOptions<MdmOptions> options, ILogger<SqliteCacheStore> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        string dbPath = options.Value.Cache.DatabasePath;
        string? directory = Path.GetDirectoryName(Path.GetFullPath(dbPath));
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        _connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = dbPath,
            Mode = SqliteOpenMode.ReadWriteCreate,
            Cache = SqliteCacheMode.Shared
        }.ToString();

        InitializeDatabase();
    }

    private void InitializeDatabase()
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        using (var pragmaCmd = connection.CreateCommand())
        {
            pragmaCmd.CommandText = "PRAGMA journal_mode=WAL; PRAGMA synchronous=NORMAL; PRAGMA busy_timeout=5000;";
            pragmaCmd.ExecuteNonQuery();
        }

        using var cmd = connection.CreateCommand();
        cmd.CommandText = """
            CREATE TABLE IF NOT EXISTS cached_items (
                id              INTEGER PRIMARY KEY AUTOINCREMENT,
                material_number TEXT    NOT NULL,
                version         INTEGER NOT NULL,
                payload_json    TEXT    NOT NULL,
                attempt_count   INTEGER NOT NULL DEFAULT 0,
                next_retry_at   INTEGER NOT NULL,
                created_at      TEXT    NOT NULL,
                updated_at      TEXT    NOT NULL,
                last_error      TEXT
            );

            CREATE UNIQUE INDEX IF NOT EXISTS idx_cached_material
                ON cached_items (material_number);

            CREATE TABLE IF NOT EXISTS dead_letter_items (
                id              INTEGER PRIMARY KEY AUTOINCREMENT,
                material_number TEXT    NOT NULL,
                version         INTEGER NOT NULL,
                payload_json    TEXT    NOT NULL,
                attempt_count   INTEGER NOT NULL,
                last_error      TEXT,
                created_at      TEXT    NOT NULL,
                dead_at         TEXT    NOT NULL
            );
            """;
        cmd.ExecuteNonQuery();

        _logger.LogInformation("SQLite cache initialized at {Path} (WAL mode)", _connectionString);
    }

    /// <inheritdoc />
    public async Task PersistBatchAsync(IReadOnlyList<MdmItem> items, string errorMessage, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(items);
        if (items.Count == 0) return;

        await _writeLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
            using var transaction = connection.BeginTransaction();

            string now = DateTime.UtcNow.ToString("O");
            long nowEpoch = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            long version = DateTime.UtcNow.Ticks;

            foreach (MdmItem item in items)
            {
                string materialNumber = item.NewSapNumber ?? string.Empty;

                // Bug 3 fix: skip items with an empty material number — they cannot be
                // uniquely identified and would collide under the unique index.
                if (string.IsNullOrEmpty(materialNumber))
                {
                    _logger.LogWarning(
                        "Skipping MdmItem with empty NewSapNumber in persist batch. Name={Name}",
                        item.Name);
                    continue;
                }

                string payload = JsonSerializer.Serialize(item, MdmJsonSerializer.Options);

                using var cmd = connection.CreateCommand();
                cmd.Transaction = transaction;
                cmd.CommandText = """
                    INSERT INTO cached_items (material_number, version, payload_json, attempt_count, next_retry_at, created_at, updated_at, last_error)
                    VALUES (@mat, @ver, @payload, 0, @retry, @now, @now, @err)
                    ON CONFLICT(material_number) DO UPDATE SET
                        version = excluded.version,
                        payload_json = excluded.payload_json,
                        attempt_count = 0,
                        next_retry_at = excluded.next_retry_at,
                        updated_at = excluded.updated_at,
                        last_error = excluded.last_error;
                    """;
                cmd.Parameters.AddWithValue("@mat", materialNumber);
                cmd.Parameters.AddWithValue("@ver", version);
                cmd.Parameters.AddWithValue("@payload", payload);
                cmd.Parameters.AddWithValue("@now", now);
                cmd.Parameters.AddWithValue("@retry", nowEpoch);
                cmd.Parameters.AddWithValue("@err", errorMessage);

                await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            }

            transaction.Commit();
            _logger.LogDebug("Persisted {Count} item(s) to cache (version base: {Version})", items.Count, DateTime.UtcNow.Ticks);
        }
        finally
        {
            _writeLock.Release();
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<CachedMdmItem>> GetDueItemsAsync(int maxCount, CancellationToken cancellationToken = default)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        using var cmd = connection.CreateCommand();
        cmd.CommandText = """
            SELECT id, material_number, version, payload_json, attempt_count, next_retry_at, created_at, updated_at, last_error
            FROM cached_items
            WHERE next_retry_at <= @now
            ORDER BY version ASC
            LIMIT @limit;
            """;
        cmd.Parameters.AddWithValue("@now", DateTimeOffset.UtcNow.ToUnixTimeSeconds());
        cmd.Parameters.AddWithValue("@limit", maxCount);

        var results = new List<CachedMdmItem>();
        using var reader = await cmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            var cached = new CachedMdmItem
            {
                Id = reader.GetInt64(0),
                MaterialNumber = reader.GetString(1),
                Version = reader.GetInt64(2),
                PayloadJson = reader.GetString(3),
                AttemptCount = reader.GetInt32(4),
                NextRetryAt = reader.GetInt64(5),
                CreatedAt = reader.GetString(6),
                UpdatedAt = reader.GetString(7),
                LastError = reader.IsDBNull(8) ? null : reader.GetString(8)
            };
            cached.Item = JsonSerializer.Deserialize<MdmItem>(cached.PayloadJson, MdmJsonSerializer.Options);
            results.Add(cached);
        }

        return results;
    }

    /// <inheritdoc />
    public async Task MarkSentAsync(IReadOnlyList<long> ids, CancellationToken cancellationToken = default)
    {
        if (ids.Count == 0) return;

        await _writeLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
            using var transaction = connection.BeginTransaction();

            foreach (long id in ids)
            {
                using var cmd = connection.CreateCommand();
                cmd.Transaction = transaction;
                cmd.CommandText = "DELETE FROM cached_items WHERE id = @id;";
                cmd.Parameters.AddWithValue("@id", id);
                await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            }

            transaction.Commit();
        }
        finally
        {
            _writeLock.Release();
        }
    }

    /// <inheritdoc />
    public async Task MarkFailedAsync(IReadOnlyList<long> ids, string errorMessage, int baseBackoffSeconds, int maxBackoffHours, CancellationToken cancellationToken = default)
    {
        if (ids.Count == 0) return;

        await _writeLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
            using var transaction = connection.BeginTransaction();

            foreach (long id in ids)
            {
                using var selectCmd = connection.CreateCommand();
                selectCmd.Transaction = transaction;
                selectCmd.CommandText = "SELECT attempt_count FROM cached_items WHERE id = @id;";
                selectCmd.Parameters.AddWithValue("@id", id);
                object? result = await selectCmd.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
                if (result is null or DBNull) continue;

                int attempts = Convert.ToInt32(result) + 1;
                double delaySeconds = Math.Min(
                    Math.Pow(2, attempts) * baseBackoffSeconds,
                    maxBackoffHours * 3600.0);
                long nextRetry = DateTimeOffset.UtcNow.ToUnixTimeSeconds() + (long)delaySeconds;
                string now = DateTime.UtcNow.ToString("O");

                using var updateCmd = connection.CreateCommand();
                updateCmd.Transaction = transaction;
                updateCmd.CommandText = """
                    UPDATE cached_items
                    SET attempt_count = @attempts, next_retry_at = @next, updated_at = @now, last_error = @err
                    WHERE id = @id;
                    """;
                updateCmd.Parameters.AddWithValue("@attempts", attempts);
                updateCmd.Parameters.AddWithValue("@next", nextRetry);
                updateCmd.Parameters.AddWithValue("@now", now);
                updateCmd.Parameters.AddWithValue("@err", errorMessage);
                updateCmd.Parameters.AddWithValue("@id", id);
                await updateCmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            }

            transaction.Commit();
        }
        finally
        {
            _writeLock.Release();
        }
    }

    /// <inheritdoc />
    public async Task MoveToDeadLetterAsync(IReadOnlyList<long> ids, CancellationToken cancellationToken = default)
    {
        if (ids.Count == 0) return;

        await _writeLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
            using var transaction = connection.BeginTransaction();

            foreach (long id in ids)
            {
                using var cmd = connection.CreateCommand();
                cmd.Transaction = transaction;
                cmd.CommandText = """
                    INSERT INTO dead_letter_items (material_number, version, payload_json, attempt_count, last_error, created_at, dead_at)
                    SELECT material_number, version, payload_json, attempt_count, last_error, created_at, @now
                    FROM cached_items WHERE id = @id;

                    DELETE FROM cached_items WHERE id = @id;
                    """;
                cmd.Parameters.AddWithValue("@id", id);
                cmd.Parameters.AddWithValue("@now", DateTime.UtcNow.ToString("O"));
                await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            }

            transaction.Commit();
            _logger.LogWarning("Moved {Count} item(s) to dead-letter table after exceeding max attempts", ids.Count);
        }
        finally
        {
            _writeLock.Release();
        }
    }

    /// <inheritdoc />
    public async Task<long> GetCacheDepthAsync(CancellationToken cancellationToken = default)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM cached_items;";
        object? result = await cmd.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
        return Convert.ToInt64(result);
    }

    /// <inheritdoc />
    public async Task CheckpointAsync(CancellationToken cancellationToken = default)
    {
        await _writeLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

            using var cmd = connection.CreateCommand();
            cmd.CommandText = "PRAGMA wal_checkpoint(TRUNCATE);";
            await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);

            _logger.LogInformation("SQLite WAL checkpoint completed");
        }
        finally
        {
            _writeLock.Release();
        }
    }

    /// <inheritdoc />
    public async Task<DeadLetterInfo> GetDeadLetterInfoAsync(CancellationToken cancellationToken = default)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        using var cmd = connection.CreateCommand();
        cmd.CommandText = """
            SELECT COUNT(*), MIN(dead_at), MAX(dead_at)
            FROM dead_letter_items;
            """;

        using var reader = await cmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        if (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            return new DeadLetterInfo
            {
                Count = reader.GetInt64(0),
                OldestDeadAt = reader.IsDBNull(1) ? null : reader.GetString(1),
                NewestDeadAt = reader.IsDBNull(2) ? null : reader.GetString(2)
            };
        }

        return new DeadLetterInfo { Count = 0 };
    }

    public async ValueTask DisposeAsync()
    {
        _writeLock.Dispose();
        await Task.CompletedTask.ConfigureAwait(false);
    }
}
