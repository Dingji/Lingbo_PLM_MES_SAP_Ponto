using System;
using System.Diagnostics;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

using PlmMesSync.Logging;
using PlmMesSync.Services;

namespace PlmMesSync.Endpoints;

public static class DashboardEndpoints
{
    private static readonly DateTime StartTime = DateTime.Now;

    public static void MapDashboardEndpoints(this WebApplication app)
    {
        app.MapGet("/api/dashboard/triggers", (TriggerStore store, int? count) =>
            store.GetRecent(count ?? 200));

        app.MapGet("/api/dashboard/syncs", (SyncStore store, int? count) =>
            store.GetRecent(count ?? 200));

        app.MapGet("/api/dashboard/logs", (BomFileLogger logger) =>
            logger.ListLogFiles());

        app.MapGet("/api/dashboard/logs/{filename}", (string filename, BomFileLogger logger) =>
        {
            var content = logger.ReadLogFile(filename);
            return content is null
                ? Results.NotFound(new { error = "File not found" })
                : Results.Text(content);
        });

        app.MapGet("/api/dashboard/status", (TriggerStore triggerStore, SyncStore syncStore,
            BomDebouncerService debouncer) =>
        {
            var uptime = DateTime.Now - StartTime;
            return new
            {
                serviceName = "PLM_MES_SAP_Ponto",
                status = "running",
                startTime = StartTime,
                uptime = $"{(int)uptime.TotalHours}h {uptime.Minutes}m {uptime.Seconds}s",
                totalTriggers = triggerStore.TotalCount,
                totalSyncs = syncStore.TotalCount,
                pendingSyncs = debouncer.PendingCount
            };
        });
    }
}
