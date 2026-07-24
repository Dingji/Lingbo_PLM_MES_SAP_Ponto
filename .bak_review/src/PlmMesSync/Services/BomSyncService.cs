using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using PlmMesSync.Data;
using PlmMesSync.Logging;
using PlmMesSync.Models.Dto;
using PlmMesSync.Models.Entities;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace PlmMesSync.Services;

public interface IBomSyncService
{
    Task SyncBom(int bomId, DateTime triggeredAt);
}

public class BomSyncService : IBomSyncService
{
    private readonly AgileDbContext _db;
    private readonly BomFileLogger _fileLogger;
    private readonly MesUploadService _mesUpload;
    private readonly SyncStore _syncStore;
    private readonly ILogger<BomSyncService> _logger;

    public BomSyncService(
        AgileDbContext db,
        BomFileLogger fileLogger,
        MesUploadService mesUpload,
        SyncStore syncStore,
        ILogger<BomSyncService> logger)
    {
        _db = db;
        _fileLogger = fileLogger;
        _mesUpload = mesUpload;
        _syncStore = syncStore;
        _logger = logger;
    }

    public async Task SyncBom(int bomId, DateTime triggeredAt)
    {
        var sw = Stopwatch.StartNew();
        _logger.LogInformation(
            "[SYNC START] BOM {BomId} | Triggered at: {TriggeredAt}",
            bomId, triggeredAt.ToString("yyyy-MM-dd HH:mm:ss.fff"));

        try
        {
            var bom = await _db.Boms.AsNoTracking()
                .FirstOrDefaultAsync(b => b.Id == bomId);

            if (bom is null)
            {
                _logger.LogWarning("[SYNC SKIP] BOM {BomId} not found (likely deleted)", bomId);
                _syncStore.Add(bomId, null, null, triggeredAt, DateTime.Now, "SKIPPED",
                    error: "BOM record not found");
                return;
            }

            var parentItem = await _db.Items.AsNoTracking()
                .FirstOrDefaultAsync(i => i.Id == bom.Item);

            _logger.LogInformation("[SYNC] Parent Item: {ItemNumber} | ID: {ItemId}",
                parentItem?.ItemNumber ?? "N/A", bom.Item);

            var allRevs = await _db.Revs.AsNoTracking()
                .Where(r => r.Item == bom.Item)
                .OrderBy(r => r.ReleaseDate)
                .ToListAsync();

            _logger.LogInformation("[SYNC] Found {RevCount} revisions for Item {ItemId}",
                allRevs.Count, bom.Item);

            var allBomLines = await _db.Boms.AsNoTracking()
                .Where(b => b.Item == bom.Item)
                .Include(b => b.RefDesigs)
                .ToListAsync();

            _logger.LogInformation("[SYNC] Found {LineCount} BOM lines for Item {ItemId}",
                allBomLines.Count, bom.Item);

            var listEntryMap = await BuildListEntryMap(allBomLines);
            var changes = await BuildChangeMap(allBomLines);
            var componentRevMap = await BuildComponentRevMap(allBomLines);

            var revChangeMap = allRevs
                .Where(r => r.Change.HasValue)
                .GroupBy(r => r.Change!.Value)
                .ToDictionary(g => g.Key, g => g.First());

            var output = new BomSyncOutput
            {
                ParentItemNumber = parentItem?.ItemNumber ?? bom.ItemNumber ?? $"ITEM_{bom.Item}",
                ParentDescription = parentItem?.Description
            };

            for (int revIdx = 0; revIdx < allRevs.Count; revIdx++)
            {
                var rev = allRevs[revIdx];
                var isLatest = rev.LatestFlag == 1 || revIdx == allRevs.Count - 1;

                var versionOutput = new RevVersionOutput
                {
                    RevNumber = rev.RevNumber ?? $"Rev_{revIdx}",
                    ReleaseDate = rev.ReleaseDate,
                    IsLatest = isLatest
                };

                var activeLines = allBomLines
                    .Where(b => IsLineActiveInRev(b, revIdx, allRevs, revChangeMap))
                    .OrderBy(b => int.TryParse(b.FindNumber, out var fn) ? fn : 0)
                    .ToList();

                foreach (var line in activeLines)
                {
                    versionOutput.Lines.Add(BuildLineOutput(line, listEntryMap, changes, componentRevMap));
                }

                output.Versions.Add(versionOutput);
            }

            if (allRevs.Count == 0)
            {
                var noRevVersion = new RevVersionOutput
                {
                    RevNumber = "(No Rev)",
                    IsLatest = true
                };

                foreach (var line in allBomLines.OrderBy(b => int.TryParse(b.FindNumber, out var fn) ? fn : 0))
                {
                    noRevVersion.Lines.Add(BuildLineOutput(line, listEntryMap, changes, componentRevMap));
                }

                output.Versions.Add(noRevVersion);
            }

            var totalLines = output.Versions.Sum(v => v.Lines.Count);
            _logger.LogInformation(
                "[SYNC] Built output | Versions: {VersionCount} | Total lines: {TotalLines}",
                output.Versions.Count, totalLines);

            var logFile = _fileLogger.WriteSyncLog(output);

            var mesResult = await _mesUpload.UploadBomToMes(output);
            _fileLogger.AppendMesSection(logFile, mesResult.Endpoint, mesResult.RequestJson,
                mesResult.HttpStatus, mesResult.ResponseBody, mesResult.Error,
                mesResult.RequestTime, mesResult.ResponseTime);

            if (!mesResult.Success)
                _logger.LogWarning("[SYNC] MES upload failed for {Item}: {Error}",
                    output.ParentItemNumber, mesResult.Error);

            sw.Stop();
            _logger.LogInformation(
                "[SYNC COMPLETE] BOM {BomId} | Item: {ItemNumber} | Log: {LogFile} | Total: {TotalMs}ms",
                bomId, output.ParentItemNumber, logFile, sw.ElapsedMilliseconds);

            _syncStore.Add(bomId, bom.Item, output.ParentItemNumber,
                triggeredAt, DateTime.Now, "SUCCESS", logFile: logFile,
                mesStatus: mesResult.Success ? "OK" : $"FAILED: {mesResult.Error}");
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex,
                "[SYNC FAILED] BOM {BomId} | Elapsed: {ElapsedMs}ms | Error: {Error}",
                bomId, sw.ElapsedMilliseconds, ex.Message);
            _syncStore.Add(bomId, null, null, triggeredAt, DateTime.Now, "FAILED",
                error: ex.Message);
        }
    }

    private static BomLineOutput BuildLineOutput(
        BomEntity line,
        Dictionary<int, string?> listEntryMap,
        Dictionary<int, ChangeEntity> changes,
        Dictionary<int, RevEntity> componentRevMap)
    {
        changes.TryGetValue(line.ChangeIn ?? 0, out var changeIn);
        changes.TryGetValue(line.ChangeOut ?? 0, out var changeOut);

        string? componentRev = null;
        string? componentRevDesc = null;
        if (line.Component.HasValue && line.Component.Value != 0
            && componentRevMap.TryGetValue(line.Component.Value, out var compRev))
        {
            componentRev = compRev.RevNumber;
            componentRevDesc = compRev.Description;
        }

        return new BomLineOutput
        {
            Level = 1,
            FindNumber = int.TryParse(line.FindNumber, out var fn) ? fn : 0,
            ItemNumber = line.ItemNumber,
            Quantity = line.Quantity,
            Description = componentRevDesc ?? line.Description,
            RefDesig = string.Join(",",
                line.RefDesigs
                    .Where(r => !string.IsNullOrEmpty(r.Label))
                    .OrderBy(r => r.Label)
                    .Select(r => r.Label!)),
            SubstituteGroup = line.List06.HasValue && line.List06.Value != 0
                ? listEntryMap.GetValueOrDefault(line.List06.Value)
                : null,
            SubstitutePriority = line.List07.HasValue && line.List07.Value != 0
                ? listEntryMap.GetValueOrDefault(line.List07.Value)
                : null,
            ChangeInNumber = changeIn?.ChangeNumber,
            ChangeOutNumber = changeOut?.ChangeNumber,
            ComponentRev = componentRev,
            ComponentRevDescription = componentRevDesc,
            HasChildren = false
        };
    }

    private async Task<Dictionary<int, string?>> BuildListEntryMap(List<BomEntity> bomLines)
    {
        var listEntryIds = bomLines
            .SelectMany(b => new[] { b.List06, b.List07 })
            .Where(id => id.HasValue && id.Value != 0)
            .Select(id => id!.Value)
            .Distinct()
            .ToList();

        if (listEntryIds.Count == 0)
            return new Dictionary<int, string?>();

        var listEntries = await _db.ListEntries.AsNoTracking()
            .Where(le => le.LangId == 4 && le.EntryId != null && listEntryIds.Contains(le.EntryId.Value))
            .ToListAsync();

        return listEntries
            .GroupBy(le => le.EntryId!.Value)
            .ToDictionary(g => g.Key, g => g.First().EntryValue);
    }

    private async Task<Dictionary<int, ChangeEntity>> BuildChangeMap(List<BomEntity> bomLines)
    {
        var changeIds = bomLines
            .SelectMany(b => new[] { b.ChangeIn, b.ChangeOut })
            .Where(id => id.HasValue && id.Value != 0)
            .Select(id => id!.Value)
            .Distinct()
            .ToList();

        if (changeIds.Count == 0)
            return new Dictionary<int, ChangeEntity>();

        return await _db.Changes.AsNoTracking()
            .Where(c => changeIds.Contains(c.Id))
            .ToDictionaryAsync(c => c.Id);
    }

    private async Task<Dictionary<int, RevEntity>> BuildComponentRevMap(List<BomEntity> bomLines)
    {
        var componentIds = bomLines
            .Where(b => b.Component.HasValue && b.Component.Value != 0)
            .Select(b => b.Component!.Value)
            .Distinct()
            .ToList();

        if (componentIds.Count == 0)
            return new Dictionary<int, RevEntity>();

        var revs = await _db.Revs.AsNoTracking()
            .Where(r => r.Item != null && componentIds.Contains(r.Item.Value))
            .ToListAsync();

        return revs
            .GroupBy(r => r.Item!.Value)
            .ToDictionary(
                g => g.Key,
                g => g.OrderByDescending(r => r.LatestFlag == 1 ? 1 : 0)
                      .ThenByDescending(r => r.ReleaseDate ?? DateTime.MinValue)
                      .First());
    }

    private static bool IsLineActiveInRev(
        BomEntity line, int currentRevIdx,
        List<RevEntity> allRevs, Dictionary<int, RevEntity> revChangeMap)
    {
        int addedAtIdx = -1;
        if (line.ChangeIn.HasValue && line.ChangeIn.Value != 0
            && revChangeMap.TryGetValue(line.ChangeIn.Value, out var addedRev))
        {
            addedAtIdx = allRevs.IndexOf(addedRev);
        }

        if (addedAtIdx == -1)
            addedAtIdx = 0;

        if (currentRevIdx < addedAtIdx)
            return false;

        if (line.ChangeOut.HasValue && line.ChangeOut.Value != 0
            && revChangeMap.TryGetValue(line.ChangeOut.Value, out var removedRev))
        {
            var removedAtIdx = allRevs.IndexOf(removedRev);
            if (removedAtIdx >= 0 && currentRevIdx >= removedAtIdx)
                return false;
        }

        return true;
    }
}
