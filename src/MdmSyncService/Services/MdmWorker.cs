using System.Threading.Channels;
using Microsoft.Extensions.Options;
using MdmSyncService.Configuration;
using MdmSyncService.Models;
using MdmSyncService.Persistence;

namespace MdmSyncService.Services;

/// <summary>
/// Background worker that continuously consumes material batches from a thread-safe channel
/// and dispatches them to the MDM platform via <see cref="IMdmSyncService"/>.
///
/// Design notes:
/// - Uses System.Threading.Channels for a bounded, back-pressure-aware producer/consumer pattern.
/// - Producers (e.g. an API controller, file watcher, or scheduled job) write batches to the channel.
/// - This worker is the sole consumer, ensuring serialized processing and predictable load.
/// - On send failure, items are persisted to a local SQLite cache for automatic retry by RetryWorker.
/// - Gracefully drains remaining items on shutdown before stopping.
/// </summary>
public sealed class MdmWorker : BackgroundService
{
    private readonly ChannelReader<MdmItem[]> _channelReader;
    private readonly IMdmSyncService _syncService;
    private readonly ICacheStore _cacheStore;
    private readonly ILogger<MdmWorker> _logger;
    private readonly int _maxInitialBatchSize;

    public MdmWorker(
        ChannelReader<MdmItem[]> channelReader,
        IMdmSyncService syncService,
        ICacheStore cacheStore,
        IOptions<MdmOptions> options,
        ILogger<MdmWorker> logger)
    {
        _channelReader = channelReader ?? throw new ArgumentNullException(nameof(channelReader));
        _syncService = syncService ?? throw new ArgumentNullException(nameof(syncService));
        _cacheStore = cacheStore ?? throw new ArgumentNullException(nameof(cacheStore));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _maxInitialBatchSize = options?.Value.Cache.MaxInitialBatchSize ?? 200;
    }

    /// <summary>
    /// Main execution loop. Reads batches from the channel until the service is stopped
    /// or the channel is completed (no more producers).
    /// </summary>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("MdmWorker started. Waiting for material batches to process...");

        try
        {
            // ReadAllAsync yields each batch as it becomes available.
            // It completes when the channel writer calls TryComplete() during shutdown.
            await foreach (MdmItem[] batch in _channelReader.ReadAllAsync(stoppingToken).ConfigureAwait(false))
            {
                await ProcessBatchAsync(batch, stoppingToken).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Expected during graceful shutdown — not an error.
            _logger.LogInformation("MdmWorker received shutdown signal. Draining complete.");
        }
        catch (Exception ex)
        {
            // Unexpected fatal error in the consumer loop.
            _logger.LogCritical(ex, "MdmWorker encountered a fatal error and will stop.");
            throw;
        }

        _logger.LogInformation("MdmWorker stopped.");
    }

    /// <summary>
    /// Processes a single batch of material items. If the batch exceeds the configured
    /// MaxInitialBatchSize it is transparently split into sub-batches to avoid
    /// oversized HTTP requests (Bug 4 fix).
    /// </summary>
    private async Task ProcessBatchAsync(MdmItem[] batch, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Processing batch of {ItemCount} material(s)...", batch.Length);

        // Bug 4 fix: split oversized initial batches to match the configured limit.
        if (_maxInitialBatchSize > 0 && batch.Length > _maxInitialBatchSize)
        {
            _logger.LogInformation(
                "Splitting batch of {Total} items into sub-batches of max {Limit}",
                batch.Length, _maxInitialBatchSize);

            for (int offset = 0; offset < batch.Length; offset += _maxInitialBatchSize)
            {
                int chunkSize = Math.Min(_maxInitialBatchSize, batch.Length - offset);
                var subBatch = new MdmItem[chunkSize];
                Array.Copy(batch, offset, subBatch, 0, chunkSize);
                await SendSingleBatchAsync(subBatch, cancellationToken).ConfigureAwait(false);
            }
        }
        else
        {
            await SendSingleBatchAsync(batch, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Sends a single (potentially already split) batch to MDM.
    /// On failure the batch is persisted to the cache store for later retry.
    /// </summary>
    private async Task SendSingleBatchAsync(MdmItem[] batch, CancellationToken cancellationToken)
    {
        try
        {
            MdmResponse response = await _syncService.SendAsync(batch, cancellationToken).ConfigureAwait(false);

            _logger.LogInformation(
                "Batch processed successfully. MDM response: code={Code}, message={Message}",
                response.Code, response.Message);
        }
        catch (MdmSyncException ex)
        {
            _logger.LogWarning(
                "Batch failed (code={MdmCode}). Persisting {ItemCount} item(s) to cache for retry. Error: {Message}",
                ex.MdmCode, batch.Length, ex.Message);

            // Bug 2 fix: pass the real cancellation token so shutdown is not blocked.
            // The cache store's write-lock will respect cancellation once the token is signalled.
            await _cacheStore.PersistBatchAsync(batch, ex.Message, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Unexpected error processing batch of {ItemCount} item(s). Persisting to cache. Error: {Message}",
                batch.Length, ex.Message);

            await _cacheStore.PersistBatchAsync(batch, ex.Message, cancellationToken).ConfigureAwait(false);
        }
    }
}
