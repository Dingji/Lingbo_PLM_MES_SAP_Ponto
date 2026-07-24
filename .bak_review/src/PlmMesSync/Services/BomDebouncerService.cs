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

public class BomDebouncerService : BackgroundService
{
    private readonly ConcurrentDictionary<int, CancellationTokenSource> _pending = new();
    private readonly TriggerStore _triggerStore;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<BomDebouncerService> _logger;
    private readonly int _delaySeconds;

    public BomDebouncerService(
        TriggerStore triggerStore,
        IServiceScopeFactory scopeFactory,
        IConfiguration configuration,
        ILogger<BomDebouncerService> logger)
    {
        _triggerStore = triggerStore;
        _scopeFactory = scopeFactory;
        _logger = logger;
        _delaySeconds = configuration.GetValue("SyncSettings:DelaySeconds", 15);
    }

    public void EnqueueTrigger(TriggerEvent evt)
    {
        _triggerStore.Add(evt.Id, evt.Action, evt.Table);

        if (_pending.TryRemove(evt.Id, out var oldCts))
        {
            oldCts.Cancel();
            oldCts.Dispose();
            _logger.LogInformation(
                "[DEBOUNCE] Previous pending sync for BOM {BomId} cancelled and reset | Pending queue size: {PendingCount}",
                evt.Id, _pending.Count);
        }

        var cts = new CancellationTokenSource();
        _pending[evt.Id] = cts;
        var triggeredAt = DateTime.Now;

        _logger.LogInformation(
            "[SCHEDULED] BOM {BomId} | Action: {Action} | Sync will execute at: {ExecuteAt} (delay: {Delay}s) | Current pending: {PendingCount}",
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
                    "[TIMER FIRED] BOM {BomId} | Delay elapsed, starting sync execution | Remaining pending: {PendingCount}",
                    evt.Id, _pending.Count);

                using var scope = _scopeFactory.CreateScope();
                var syncService = scope.ServiceProvider.GetRequiredService<IBomSyncService>();
                await syncService.SyncBom(evt.Id, triggeredAt);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation(
                    "[DEBOUNCED] BOM {BomId} | Timer cancelled by newer trigger (expected behavior)",
                    evt.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "[ERROR] BOM {BomId} | Unexpected error during sync execution | Error: {Error}",
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
            "[SERVICE] BomDebouncerService started | Delay setting: {Delay}s | Listening for BOM change triggers",
            _delaySeconds);
        return Task.CompletedTask;
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "[SHUTDOWN] BomDebouncerService stopping | Cancelling {PendingCount} pending sync(s)",
            _pending.Count);

        foreach (var kvp in _pending)
        {
            _logger.LogInformation("[SHUTDOWN] Cancelling pending sync for BOM {BomId}", kvp.Key);
            kvp.Value.Cancel();
            kvp.Value.Dispose();
        }
        _pending.Clear();

        await base.StopAsync(cancellationToken);
        _logger.LogInformation("[SHUTDOWN] BomDebouncerService stopped gracefully");
    }
}
