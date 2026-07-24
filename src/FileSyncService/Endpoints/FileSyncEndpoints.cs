using FileSyncService.Models.Dto;
using FileSyncService.Services;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using System.Diagnostics;
using System.Text.Json;

namespace FileSyncService.Endpoints;

/// <summary>
/// Maps the /fileSync HTTP endpoint for file download and analysis.
/// </summary>
public static class FileSyncEndpoints
{
    /// <summary>
    /// Registers the /fileSync endpoint on the application.
    /// </summary>
    /// <param name="app">The web application instance.</param>
    public static void MapFileSyncEndpoints(this WebApplication app)
    {
        app.MapPost("/fileSync", HandleFileSyncAsync)
            .WithName("FileSync")
            .WithSummary("Downloads and analyzes files from provided URLs.")
            .Accepts<FileSyncRequest>("application/json")
            .Produces<FileSyncResponse>(StatusCodes.Status200OK)
            .Produces<FileSyncResponse>(StatusCodes.Status400BadRequest);
    }

    /// <summary>
    /// Handles the POST /fileSync request.
    /// Downloads all specified files, analyzes them, and returns detailed metadata.
    /// </summary>
    private static async Task<IResult> HandleFileSyncAsync(
        HttpContext context,
        RequestStore requestStore,
        FileSyncProcessor processor,
        MesFileUploadService mesFileUploadService,
        FileSyncFileLogger fileLogger,
        ILogger<Program> logger)
    {
        var requestId = Guid.NewGuid().ToString("N")[..12];
        var stopwatch = Stopwatch.StartNew();
        var receivedTime = DateTime.UtcNow;
        var clientIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        logger.LogInformation("[FileSync] ===== Request {RequestId} received from {ClientIp} =====", requestId, clientIp);

        // Parse the request body.
        FileSyncRequest? request;
        try
        {
            request = await JsonSerializer.DeserializeAsync<FileSyncRequest>(
                context.Request.Body,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "[FileSync] Request {RequestId}: Invalid JSON body", requestId);
            var errorResponse = new FileSyncResponse
            {
                RequestId = requestId,
                ReceivedTimeUtc = receivedTime,
                CompletedTimeUtc = DateTime.UtcNow,
                DurationMs = stopwatch.ElapsedMilliseconds,
                Status = "Failed",
                ErrorMessage = "Invalid JSON format in request body"
            };

            RecordFailedRequest(requestStore, requestId, receivedTime, clientIp, errorResponse.ErrorMessage);
            return Results.BadRequest(errorResponse);
        }

        if (request is null || request.Files is null || request.Files.Count == 0)
        {
            logger.LogWarning("[FileSync] Request {RequestId}: Empty or null files array", requestId);
            var errorResponse = new FileSyncResponse
            {
                RequestId = requestId,
                ReceivedTimeUtc = receivedTime,
                CompletedTimeUtc = DateTime.UtcNow,
                DurationMs = stopwatch.ElapsedMilliseconds,
                Status = "Failed",
                ErrorMessage = "The 'files' array is empty or missing."
            };

            RecordFailedRequest(requestStore, requestId, receivedTime, clientIp, errorResponse.ErrorMessage);
            return Results.BadRequest(errorResponse);
        }

        logger.LogInformation("[FileSync] Request {RequestId}: Processing {Count} file(s)", requestId, request.Files.Count);

        // Create the request record for the dashboard.
        var record = new RequestRecord
        {
            RequestId = requestId,
            ReceivedTimeUtc = receivedTime,
            Status = "Processing",
            TotalFiles = request.Files.Count,
            ClientIp = clientIp
        };
        requestStore.Add(record);

        // Process all files concurrently with controlled parallelism.
        var tasks = request.Files.Select(file => processor.ProcessFileAsync(file, context.RequestAborted));
        var results = await Task.WhenAll(tasks);
        var resultList = results.ToList();

        // Upload processed files to MES and capture upload records.
        List<MesUploadRecord> mesRecords;
        try
        {
            mesRecords = await mesFileUploadService.UploadFilesToMes(resultList, context.RequestAborted);
        }
        catch (Exception ex)
        {
            mesRecords = [];
            logger.LogError(ex, "[FileSync] Request {RequestId}: MES file upload failed", requestId);
        }

        stopwatch.Stop();

        // Determine overall status.
        var successCount = resultList.Count(r => r.Success);
        var failedCount = resultList.Count(r => !r.Success);
        var status = failedCount == 0 ? "Success" : successCount == 0 ? "Failed" : "PartialSuccess";

        // Build the response.
        var response = new FileSyncResponse
        {
            RequestId = requestId,
            ReceivedTimeUtc = receivedTime,
            CompletedTimeUtc = DateTime.UtcNow,
            DurationMs = stopwatch.ElapsedMilliseconds,
            Status = status,
            TotalFiles = request.Files.Count,
            SuccessCount = successCount,
            FailedCount = failedCount,
            Results = resultList
        };

        // Update the dashboard record.
        record.CompletedTimeUtc = response.CompletedTimeUtc;
        record.DurationMs = response.DurationMs;
        record.Status = status;
        record.SuccessCount = successCount;
        record.FailedCount = failedCount;
        record.Results = resultList;

        // Write the detailed request log file.
        var logFilePath = fileLogger.WriteRequestLog(requestId, receivedTime, clientIp, request, response);

        // Append MES upload details to the same log file.
        foreach (var mesRecord in mesRecords)
        {
            fileLogger.AppendMesSection(
                logFilePath,
                mesRecord.Endpoint,
                mesRecord.RequestJson,
                mesRecord.HttpStatus,
                mesRecord.ResponseBody,
                mesRecord.Error,
                mesRecord.RequestTime,
                mesRecord.ResponseTime);
        }

        record.LogFile = Path.GetFileName(logFilePath);
        requestStore.Update(record);

        logger.LogInformation(
            "[FileSync] Request {RequestId} completed: Status={Status}, Success={Success}, Failed={Failed}, Duration={Duration}ms",
            requestId, status, successCount, failedCount, response.DurationMs);

        return Results.Ok(response);
    }

    /// <summary>
    /// Records a failed request in the store for dashboard visibility.
    /// </summary>
    private static void RecordFailedRequest(
        RequestStore store, string requestId, DateTime receivedTime, string clientIp, string? errorMessage)
    {
        store.Add(new RequestRecord
        {
            RequestId = requestId,
            ReceivedTimeUtc = receivedTime,
            CompletedTimeUtc = DateTime.UtcNow,
            DurationMs = 0,
            Status = "Failed",
            TotalFiles = 0,
            ClientIp = clientIp,
            ErrorMessage = errorMessage
        });
    }
}
