using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using PlmMesSync.Models.Dto;

namespace PlmMesSync.Services;

public class FileSyncDebouncerService : BackgroundService
{
    private readonly ConcurrentDictionary<int, CancellationTokenSource> _pending = new();
    private readonly TriggerStore _triggerStore;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<FileSyncDebouncerService> _logger;
    private readonly int _delaySeconds;

    public FileSyncDebouncerService(
        TriggerStore triggerStore,
        IServiceScopeFactory scopeFactory,
        IConfiguration configuration,
        ILogger<FileSyncDebouncerService> logger)
    {
        _triggerStore = triggerStore;
        _scopeFactory = scopeFactory;
        _logger = logger;
        _delaySeconds = configuration.GetValue("FileSyncSettings:DelaySeconds", 15);
    }

    public void EnqueueTrigger(TriggerEvent evt)
    {
        _triggerStore.Add(evt.Id, evt.Action, evt.Table);

        if (_pending.TryRemove(evt.Id, out var oldCts))
        {
            oldCts.Cancel();
            oldCts.Dispose();
            _logger.LogInformation(
                "[FILE-DEBOUNCE] Previous pending file sync for FILES {FileId} cancelled and reset | Pending: {Count}",
                evt.Id, _pending.Count);
        }

        var cts = new CancellationTokenSource();
        _pending[evt.Id] = cts;
        var triggeredAt = DateTime.Now;

        _logger.LogInformation(
            "[FILE-SCHEDULED] FILES {FileId} | Action: {Action} | Sync at: {ExecuteAt} (delay: {Delay}s) | Pending: {Count}",
            evt.Id, evt.Action,
            triggeredAt.AddSeconds(_delaySeconds).ToString("yyyy-MM-dd HH:mm:ss"),
            _delaySeconds, _pending.Count);

        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(_delaySeconds), cts.Token);
                _pending.TryRemove(evt.Id, out _);

                _logger.LogInformation(
                    "[FILE-TIMER FIRED] FILES {FileId} | Starting file sync | Remaining pending: {Count}",
                    evt.Id, _pending.Count);

                using var scope = _scopeFactory.CreateScope();
                var syncService = scope.ServiceProvider.GetRequiredService<FileSyncUploadService>();
                await syncService.SyncFile(evt.Id, triggeredAt);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation(
                    "[FILE-DEBOUNCED] FILES {FileId} | Timer cancelled by newer trigger",
                    evt.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "[FILE-ERROR] FILES {FileId} | Unexpected error during file sync | Error: {Error}",
                    evt.Id, ex.Message);
            }
            finally
            {
                cts.Dispose();
            }
        }, CancellationToken.None);
    }

    public int PendingCount => _pending.Count;

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "[SERVICE] FileSyncDebouncerService started | Delay: {Delay}s | Listening for FILES change triggers",
            _delaySeconds);
        return Task.CompletedTask;
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("[SHUTDOWN] FileSyncDebouncerService stopping | Cancelling {Count} pending", _pending.Count);

        foreach (var kvp in _pending)
        {
            kvp.Value.Cancel();
            kvp.Value.Dispose();
        }
        _pending.Clear();

        await base.StopAsync(cancellationToken);
        _logger.LogInformation("[SHUTDOWN] FileSyncDebouncerService stopped");
    }
}
