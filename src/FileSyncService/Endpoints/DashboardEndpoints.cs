using FileSyncService.Services;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace FileSyncService.Endpoints;

public static class DashboardEndpoints
{
    public static void MapDashboardEndpoints(this WebApplication app)
    {
        app.MapGet("/api/dashboard/requests", (RequestStore store, int? count) =>
        {
            var records = store.GetRecent(count ?? 100);
            return Results.Ok(new { success = true, data = records });
        })
        .WithName("GetDashboardRequests");

        app.MapGet("/api/dashboard/requests/{requestId}", (string requestId, RequestStore store) =>
        {
            var record = store.GetById(requestId);
            if (record is null)
            {
                return Results.NotFound(new { success = false, error = $"Request '{requestId}' not found." });
            }
            return Results.Ok(new { success = true, data = record });
        })
        .WithName("GetDashboardRequestById");

        app.MapGet("/api/dashboard/status", (RequestStore store) =>
        {
            var (total, success, failed, processing) = store.GetStats();
            return Results.Ok(new
            {
                success = true,
                data = new
                {
                    service = "FileSyncService",
                    port = 31457,
                    uptime = (DateTime.UtcNow - System.Diagnostics.Process.GetCurrentProcess().StartTime.ToUniversalTime()).TotalSeconds,
                    totalRequests = total,
                    successRequests = success,
                    failedRequests = failed,
                    processingRequests = processing,
                    serverTimeUtc = DateTime.UtcNow
                }
            });
        })
        .WithName("GetDashboardStatus");

        app.MapGet("/api/dashboard/logs", (LogReaderService logReader) =>
            logReader.ListLogFiles());

        app.MapGet("/api/dashboard/logs/{filename}", (string filename, LogReaderService logReader) =>
        {
            var content = logReader.ReadLogFile(filename);
            return content is null
                ? Results.NotFound(new { error = "File not found" })
                : Results.Text(content);
        });
    }
}
