using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

using PlmMesSync.Models.Dto;
using PlmMesSync.Services;

using System;

namespace PlmMesSync.Endpoints;

public static class TriggerEndpoints
{
    public static void MapTriggerEndpoints(this WebApplication app)
    {
        app.MapPost("/api/trigger", HandleTrigger);
    }

    private static IResult HandleTrigger(TriggerEvent evt, BomDebouncerService bomDebouncer,
        FileSyncDebouncerService fileDebouncer, HttpContext ctx, ILoggerFactory loggerFactory)
    {
        var logger = loggerFactory.CreateLogger("PlmMesSync.Endpoints.Trigger");
        var remoteIp = ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        if (evt is null)
        {
            logger.LogWarning("[REJECTED] Null trigger event from {RemoteIP}", remoteIp);
            return Results.BadRequest(new { error = "Request body is required" });
        }

        if (string.IsNullOrWhiteSpace(evt.Table))
        {
            logger.LogWarning("[REJECTED] Trigger event with null/empty table from {RemoteIP} | ID: {Id}", remoteIp, evt.Id);
            return Results.BadRequest(new { error = "'table' field is required" });
        }

        if (string.IsNullOrWhiteSpace(evt.Action))
        {
            logger.LogWarning("[REJECTED] Trigger event with null/empty action from {RemoteIP} | ID: {Id}", remoteIp, evt.Id);
            return Results.BadRequest(new { error = "'action' field is required" });
        }

        logger.LogInformation(
            "[REQUEST] Received trigger notification | RemoteIP: {RemoteIP} | ID: {Id} | Action: {Action} | Table: {Table}",
            remoteIp, evt.Id, evt.Action, evt.Table);

        if (string.Equals(evt.Table, AppConstants.TableBom, StringComparison.OrdinalIgnoreCase))
        {
            bomDebouncer.EnqueueTrigger(evt);
            logger.LogInformation(
                "[ACCEPTED] BOM trigger queued | BOM ID: {Id} | Action: {Action}",
                evt.Id, evt.Action);
            return Results.Ok(new { message = "accepted", table = "BOM", id = evt.Id, action = evt.Action });
        }

        if (string.Equals(evt.Table, AppConstants.TableFiles, StringComparison.OrdinalIgnoreCase))
        {
            fileDebouncer.EnqueueTrigger(evt);
            logger.LogInformation(
                "[ACCEPTED] FILES trigger queued | File ID: {Id} | Action: {Action}",
                evt.Id, evt.Action);
            return Results.Ok(new { message = "accepted", table = "FILES", id = evt.Id, action = evt.Action });
        }

        logger.LogWarning(
            "[IGNORED] Table '{Table}' is not BOM or FILES, ignoring | ID: {Id} | Action: {Action}",
            evt.Table, evt.Id, evt.Action);
        return Results.Ok(new { message = "ignored", reason = $"table '{evt.Table}' not handled" });
    }
}
