using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using PlmMesSync.Data;

using System;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace PlmMesSync.Services;

public class FileSyncUploadService
{
    private readonly AgileDbContext _db;
    private readonly HttpClient _httpClient;
    private readonly SyncStore _syncStore;
    private readonly ILogger<FileSyncUploadService> _logger;
    private readonly string _fileSyncServiceUrl;
    private const int MaxRetries = 3;

    public FileSyncUploadService(
        AgileDbContext db,
        HttpClient httpClient,
        SyncStore syncStore,
        IConfiguration configuration,
        ILogger<FileSyncUploadService> logger)
    {
        _db = db;
        _httpClient = httpClient;
        _syncStore = syncStore;
        _logger = logger;
        _fileSyncServiceUrl = configuration.GetValue<string>("FileSyncSettings:FileSyncServiceUrl")
            ?? AppConstants.DefaultFileSyncServiceUrl;
    }

    public async Task SyncFile(int fileId, DateTime triggeredAt)
    {
        var sw = Stopwatch.StartNew();
        _logger.LogInformation("[FILE-SYNC START] FILES {FileId} | Triggered at: {TriggeredAt}",
            fileId, triggeredAt.ToString("yyyy-MM-dd HH:mm:ss.fff"));

        try
        {
            var combinedQuery = await (
                from vfm in _db.VersionFileMaps.AsNoTracking()
                join v in _db.Versions.AsNoTracking() on vfm.VersionId equals v.Id
                where vfm.FileId == fileId
                select new { VersionFileMap = vfm, Version = v }
            ).ToListAsync();

            _logger.LogInformation(
                "[FILE-SYNC STEP1-2] FILES {FileId} → VERSION_FILE_MAP + VERSION (JOIN) | Found {Count} record(s)",
                fileId, combinedQuery.Count);

            if (combinedQuery.Count == 0)
            {
                _logger.LogWarning("[FILE-SYNC SKIP] FILES {FileId} | No VERSION_FILE_MAP records found", fileId);
                _syncStore.Add(fileId, null, null, triggeredAt, DateTime.Now, "SKIPPED",
                    error: "No VERSION_FILE_MAP records found", table: "FILES");
                return;
            }

            var validVersions = combinedQuery
                .Where(x => x.Version.AttachId.HasValue && x.Version.VersionNum.HasValue)
                .Select(x => x.Version)
                .Distinct()
                .ToList();

            if (validVersions.Count == 0)
            {
                _logger.LogWarning("[FILE-SYNC SKIP] FILES {FileId} | No valid VERSION records (missing ATTACH_ID or VERSION_NUM)", fileId);
                _syncStore.Add(fileId, null, null, triggeredAt, DateTime.Now, "SKIPPED",
                    error: "No valid VERSION records", table: "FILES");
                return;
            }

            var attachIds = validVersions
                .Select(v => v.AttachId!.Value)
                .Distinct()
                .ToList();

            _logger.LogInformation(
                "[FILE-SYNC STEP3] VERSION → ATTACHMENT_MAP | ATTACH_IDs: [{AttachIds}]",
                string.Join(", ", attachIds));

            int? itemId = null;
            var attachmentMaps = await _db.AttachmentMaps.AsNoTracking()
                .Where(am => am.AttachId != null && attachIds.Contains(am.AttachId.Value))
                .ToListAsync();

            _logger.LogInformation(
                "[FILE-SYNC STEP3] ATTACHMENT_MAP query | Found {Count} record(s)",
                attachmentMaps.Count);

            foreach (var ver in validVersions)
            {
                var match = attachmentMaps.FirstOrDefault(am =>
                    am.AttachId == ver.AttachId!.Value && am.Version == ver.VersionNum!.Value);

                if (match is not null && match.ParentId.HasValue && match.ParentId.Value > 0)
                {
                    itemId = match.ParentId;
                    _logger.LogInformation(
                        "[FILE-SYNC STEP3] MATCH found | VERSION(ATTACH_ID={AttachId}, VERSION_NUM={VerNum}) → PARENT_ID={ParentId}",
                        ver.AttachId.Value, ver.VersionNum.Value, match.ParentId.Value);
                    break;
                }
            }

            string itemNumber = "";
            if (itemId.HasValue)
            {
                var item = await _db.Items.AsNoTracking()
                    .FirstOrDefaultAsync(i => i.Id == itemId.Value);

                _logger.LogInformation(
                    "[FILE-SYNC STEP4] ATTACHMENT_MAP.PARENT_ID={ItemId} → ITEM | Found: {Found} | ITEM_NUMBER: {ItemNumber}",
                    itemId.Value, item is not null ? "YES" : "NO", item?.ItemNumber ?? "(null)");

                if (!string.IsNullOrWhiteSpace(item?.ItemNumber))
                    itemNumber = item.ItemNumber;
                else
                    itemNumber = $"ITEM_{itemId}";
            }
            else
            {
                _logger.LogWarning(
                    "[FILE-SYNC STEP4] FILES {FileId} | Could not resolve Item — chain broken at ATTACHMENT_MAP.PARENT_ID",
                    fileId);
            }

            var fileQuery = await (
                from f in _db.Files.AsNoTracking()
                join fi in _db.FileInfos.AsNoTracking() on f.Id equals fi.FileId into fiJoin
                from fi in fiJoin.DefaultIfEmpty()
                where f.Id == fileId
                select new { File = f, FileInfo = fi }
            ).FirstOrDefaultAsync();

            var fileName = fileQuery?.File?.FileName ?? $"file_{fileId}";
            var relativePath = fileQuery?.FileInfo?.IfsFilepath ?? string.Empty;

            _logger.LogInformation(
                "[FILE-SYNC STEP5] FILES {FileId} | FileName: {FileName} | IFS_FILEPATH: {Path} | Resolved Item: {ItemNumber}",
                fileId, fileName,
                string.IsNullOrWhiteSpace(relativePath) ? "(EMPTY)" : relativePath,
                string.IsNullOrWhiteSpace(itemNumber) ? "(EMPTY)" : itemNumber);

            if (string.IsNullOrWhiteSpace(relativePath))
            {
                _logger.LogWarning("[FILE-SYNC] FILES {FileId} | IFS_FILEPATH is empty, FileSyncService may not locate the file", fileId);
            }

            var request = new
            {
                files = new[]
                {
                    new
                    {
                        inventoryCode = itemNumber,
                        fileCode = itemNumber,
                        fileName = fileName,
                        remark = $"Triggered at {triggeredAt:yyyy-MM-dd HH:mm:ss}",
                        relativePath = relativePath
                    }
                }
            };

            var (success, statusCode, responseBody) = await SendWithRetryAsync(request, fileId);

            if (success)
            {
                _logger.LogInformation(
                    "[FILE-SYNC COMPLETE] FILES {FileId} | Item: {ItemNumber} | Status: {Status}",
                    fileId, itemNumber, statusCode);

                _syncStore.Add(fileId, itemId, itemNumber, triggeredAt, DateTime.Now, "SUCCESS",
                    mesStatus: $"FileSync: {statusCode}", table: "FILES");
            }
            else
            {
                _logger.LogWarning(
                    "[FILE-SYNC FAILED] FILES {FileId} | Status: {Status} | Response: {Response}",
                    fileId, statusCode, responseBody);

                _syncStore.Add(fileId, itemId, itemNumber, triggeredAt, DateTime.Now, "FAILED",
                    error: $"FileSyncService returned {statusCode}: {responseBody}", table: "FILES");
            }

            sw.Stop();
            _logger.LogInformation("[FILE-SYNC DONE] FILES {FileId} | Total: {Ms}ms", fileId, sw.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex, "[FILE-SYNC ERROR] FILES {FileId} | Elapsed: {Ms}ms | Error: {Error}",
                fileId, sw.ElapsedMilliseconds, ex.Message);
            _syncStore.Add(fileId, null, null, triggeredAt, DateTime.Now, "FAILED", error: ex.Message, table: "FILES");
        }
    }

    private async Task<(bool Success, int StatusCode, string ResponseBody)> SendWithRetryAsync(object request, int fileId)
    {
        for (int attempt = 1; attempt <= MaxRetries; attempt++)
        {
            try
            {
                _logger.LogInformation("[FILE-SYNC] Sending to FileSyncService (attempt {Attempt}/{Max}): {Url}",
                    attempt, MaxRetries, _fileSyncServiceUrl);

                var response = await _httpClient.PostAsJsonAsync(_fileSyncServiceUrl, request);
                var responseBody = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                    return (true, (int)response.StatusCode, responseBody);

                if (attempt < MaxRetries && (int)response.StatusCode >= 500)
                {
                    _logger.LogWarning("[FILE-SYNC] Server error {Status}, retrying in {Delay}s...",
                        (int)response.StatusCode, attempt * 3);
                    await Task.Delay(TimeSpan.FromSeconds(attempt * 3));
                    continue;
                }

                return (false, (int)response.StatusCode, responseBody);
            }
            catch (HttpRequestException ex) when (attempt < MaxRetries)
            {
                _logger.LogWarning("[FILE-SYNC] HTTP error on attempt {Attempt}/{Max}: {Error}, retrying in {Delay}s...",
                    attempt, MaxRetries, ex.Message, attempt * 3);
                await Task.Delay(TimeSpan.FromSeconds(attempt * 3));
            }
            catch (TaskCanceledException ex) when (attempt < MaxRetries && !ex.CancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning("[FILE-SYNC] Timeout on attempt {Attempt}/{Max}, retrying in {Delay}s...",
                    attempt, MaxRetries, attempt * 3);
                await Task.Delay(TimeSpan.FromSeconds(attempt * 3));
            }
        }

        return (false, 0, "All retry attempts exhausted");
    }
}
