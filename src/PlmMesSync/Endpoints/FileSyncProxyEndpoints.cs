using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace PlmMesSync.Endpoints;

public static class FileSyncProxyEndpoints
{
    public static void MapFileSyncProxyEndpoints(this WebApplication app)
    {
        app.MapGet("/api/dashboard/filesync/requests", async (
            IHttpClientFactory factory, int? count, ILoggerFactory loggerFactory) =>
        {
            var client = factory.CreateClient("FileSyncDashboard");
            var logger = loggerFactory.CreateLogger("PlmMesSync.Proxy.FileSync");
            try
            {
                var response = await client.GetAsync($"/api/dashboard/requests?count={count ?? 100}");
                var body = await response.Content.ReadAsStringAsync();
                return Results.Content(body, "application/json", statusCode: (int)response.StatusCode);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "[PROXY] Failed to reach FileSyncService dashboard");
                return Results.Json(new { success = false, error = "FileSyncService unreachable" }, statusCode: 502);
            }
        });

        app.MapGet("/api/dashboard/filesync/requests/{requestId}", async (
            string requestId, IHttpClientFactory factory, ILoggerFactory loggerFactory) =>
        {
            var client = factory.CreateClient("FileSyncDashboard");
            var logger = loggerFactory.CreateLogger("PlmMesSync.Proxy.FileSync");
            try
            {
                var response = await client.GetAsync($"/api/dashboard/requests/{Uri.EscapeDataString(requestId)}");
                var body = await response.Content.ReadAsStringAsync();
                return Results.Content(body, "application/json", statusCode: (int)response.StatusCode);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "[PROXY] Failed to reach FileSyncService dashboard");
                return Results.Json(new { success = false, error = "FileSyncService unreachable" }, statusCode: 502);
            }
        });

        app.MapGet("/api/dashboard/filesync/status", async (
            IHttpClientFactory factory, ILoggerFactory loggerFactory) =>
        {
            var client = factory.CreateClient("FileSyncDashboard");
            var logger = loggerFactory.CreateLogger("PlmMesSync.Proxy.FileSync");
            try
            {
                var response = await client.GetAsync("/api/dashboard/status");
                var body = await response.Content.ReadAsStringAsync();
                return Results.Content(body, "application/json", statusCode: (int)response.StatusCode);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "[PROXY] Failed to reach FileSyncService dashboard");
                return Results.Json(new { success = false, error = "FileSyncService unreachable" }, statusCode: 502);
            }
        });

        app.MapGet("/api/dashboard/filesync/logs", async (
            IHttpClientFactory factory, ILoggerFactory loggerFactory) =>
        {
            var client = factory.CreateClient("FileSyncDashboard");
            var logger = loggerFactory.CreateLogger("PlmMesSync.Proxy.FileSync");
            try
            {
                var response = await client.GetAsync("/api/dashboard/logs");
                var body = await response.Content.ReadAsStringAsync();
                var contentType = response.Content.Headers.ContentType?.ToString() ?? "application/json";
                return Results.Content(body, contentType, statusCode: (int)response.StatusCode);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "[PROXY] Failed to reach FileSyncService dashboard");
                return Results.Json(new { error = "FileSyncService unreachable" }, statusCode: 502);
            }
        });

        app.MapGet("/api/dashboard/filesync/logs/{filename}", async (
            string filename, IHttpClientFactory factory, ILoggerFactory loggerFactory) =>
        {
            var client = factory.CreateClient("FileSyncDashboard");
            var logger = loggerFactory.CreateLogger("PlmMesSync.Proxy.FileSync");
            try
            {
                var response = await client.GetAsync($"/api/dashboard/logs/{Uri.EscapeDataString(filename)}");
                var body = await response.Content.ReadAsStringAsync();
                var contentType = response.Content.Headers.ContentType?.ToString() ?? "text/plain";
                return Results.Content(body, contentType, statusCode: (int)response.StatusCode);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "[PROXY] Failed to reach FileSyncService dashboard");
                return Results.Text("FileSyncService unreachable", statusCode: 502);
            }
        });
    }
}
