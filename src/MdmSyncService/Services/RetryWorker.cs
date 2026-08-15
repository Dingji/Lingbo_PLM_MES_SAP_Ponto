using Microsoft.Extensions.Options;
using MdmSyncService.Configuration;
using MdmSyncService.Models;
using MdmSyncService.Persistence;

namespace MdmSyncService.Services;

/// <summary>
/// Background service that periodically scans the SQLite cache for items due for retry,
/// deduplicates by material number (latest version wins), sends them to MDM, and updates
/// the cache based on success or failure. Items exceeding MaxAttempts are moved to dead-letter.
/// </summary>
public sealed class RetryWorker : BackgroundService
{
    private readonly ICacheStore _cacheStore;
    private readonly IMdmSyncService _syncService;
    private readonly MdmOptions.CacheOptions _cacheOptions;
    private readonly ILogger<RetryWorker> _logger;

    public RetryWorker(
        ICacheStore cacheStore,
        IMdmSyncService syncService,
        IOptions<MdmOptions> options,
        ILogger<RetryWorker> logger)
    {
        _cacheStore = cacheStore ?? throw new ArgumentNullException(nameof(cacheStore));
        _syncService = syncService ?? throw new ArgumentNullException(nameof(syncService));
        _cacheOptions = options?.Value.Cache ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "RetryWorker started. Scan interval: {Interval}s, max batch: {Batch}, max attempts: {Max}",
            _cacheOptions.RetryIntervalSeconds, _cacheOptions.MaxBatchSize, _cacheOptions.MaxAttempts);

        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(_cacheOptions.RetryIntervalSeconds));

        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false))
            {
                await ScanAndRetryAsync(stoppingToken).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("RetryWorker received shutdown signal.");
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "RetryWorker encountered a fatal error and will stop.");
            throw;
        }

        _logger.LogInformation("RetryWorker stopped.");
    }

    private async Task ScanAndRetryAsync(CancellationToken cancellationToken)
    {
        IReadOnlyList<CachedMdmItem> dueItems = await _cacheStore
            .GetDueItemsAsync(_cacheOptions.MaxBatchSize, cancellationToken)
            .ConfigureAwait(false);

        if (dueItems.Count == 0) return;

        _logger.LogInformation("RetryWorker found {Count} item(s) due for retry.", dueItems.Count);

        // Separate items that exceeded max attempts from those still eligible
        var deadLetterIds = new List<long>();
        var retryable = new List<CachedMdmItem>();

        foreach (CachedMdmItem item in dueItems)
        {
            if (item.AttemptCount >= _cacheOptions.MaxAttempts)
            {
                deadLetterIds.Add(item.Id);
            }
            else
            {
                retryable.Add(item);
            }
        }

        if (deadLetterIds.Count > 0)
        {
            await _cacheStore.MoveToDeadLetterAsync(deadLetterIds, cancellationToken).ConfigureAwait(false);

            // Bug 1 fix: surface dead-letter items so operators are aware of permanently-failed data.
            var deadInfo = await _cacheStore.GetDeadLetterInfoAsync(cancellationToken).ConfigureAwait(false);
            _logger.LogWarning(
                "Dead-letter queue has {Count} item(s). Oldest: {Oldest}, Newest: {Newest}. " +
                "These items exceeded {MaxAttempts} retry attempts and require manual intervention.",
                deadInfo.Count, deadInfo.OldestDeadAt ?? "N/A", deadInfo.NewestDeadAt ?? "N/A",
                _cacheOptions.MaxAttempts);
        }

        if (retryable.Count == 0) return;

        // Build the send list from deserialized payloads
        var sendItems = new List<MdmItem>();
        var idMap = new List<long>();

        foreach (CachedMdmItem cached in retryable)
        {
            if (cached.Item is not null)
            {
                sendItems.Add(cached.Item);
                idMap.Add(cached.Id);
            }
        }

        if (sendItems.Count == 0) return;

        try
        {
            MdmResponse response = await _syncService.SendAsync(sendItems, cancellationToken).ConfigureAwait(false);

            await _cacheStore.MarkSentAsync(idMap, cancellationToken).ConfigureAwait(false);

            _logger.LogInformation(
                "RetryWorker successfully sent {Count} cached item(s). MDM response: code={Code}, message={Message}",
                sendItems.Count, response.Code, response.Message);
        }
        catch (MdmSyncException ex)
        {
            _logger.LogWarning(
                "RetryWorker batch failed (code={MdmCode}). Scheduling next retry with backoff. Error: {Message}",
                ex.MdmCode, ex.Message);

            // Bug 2 fix: pass the real cancellation token so shutdown is not blocked.
            await _cacheStore.MarkFailedAsync(
                idMap, ex.Message,
                _cacheOptions.BaseBackoffSeconds, _cacheOptions.MaxBackoffHours,
                cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "RetryWorker unexpected error during send. Scheduling backoff.");

            await _cacheStore.MarkFailedAsync(
                idMap, ex.Message,
                _cacheOptions.BaseBackoffSeconds, _cacheOptions.MaxBackoffHours,
                cancellationToken).ConfigureAwait(false);
        }
    }
}
