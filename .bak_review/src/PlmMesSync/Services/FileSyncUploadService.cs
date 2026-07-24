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
        _httpClient.Timeout = TimeSpan.FromSeconds(60);
        _syncStore = syncStore;
        _logger = logger;
        _fileSyncServiceUrl = configuration.GetValue<string>("FileSyncSettings:FileSyncServiceUrl")
            ?? "http://10.170.9.4:31457/fileSync";
    }

    public async Task SyncFile(int fileId, DateTime triggeredAt)
    {
        var sw = Stopwatch.StartNew();
        _logger.LogInformation("[FILE-SYNC START] FILES {FileId} | Triggered at: {TriggeredAt}",
            fileId, triggeredAt.ToString("yyyy-MM-dd HH:mm:ss.fff"));

        try
        {
            // Step 1: FILES.ID → VERSION_FILE_MAP.FILE_ID
            var versionFileMaps = await _db.VersionFileMaps.AsNoTracking()
                .Where(vfm => vfm.FileId == fileId)
                .ToListAsync();

            _logger.LogInformation(
                "[FILE-SYNC STEP1] FILES {FileId} → VERSION_FILE_MAP | Found {Count} record(s) | VERSION_IDs: [{Ids}]",
                fileId, versionFileMaps.Count,
                string.Join(", ", versionFileMaps.Select(v => v.VersionId?.ToString() ?? "NULL")));

            if (versionFileMaps.Count == 0)
            {
                _logger.LogWarning("[FILE-SYNC SKIP] FILES {FileId} | No VERSION_FILE_MAP records found", fileId);
                _syncStore.Add(fileId, null, null, triggeredAt, DateTime.Now, "SKIPPED",
                    error: "No VERSION_FILE_MAP records found", table: "FILES");
                return;
            }

            // Step 2: VERSION_FILE_MAP.VERSION_ID → VERSION.ID
            var versionIds = versionFileMaps
                .Where(v => v.VersionId.HasValue)
                .Select(v => v.VersionId!.Value)
                .Distinct()
                .ToList();

            if (versionIds.Count == 0)
            {
                _logger.LogWarning("[FILE-SYNC SKIP] FILES {FileId} | VERSION_FILE_MAP has no valid VERSION_ID", fileId);
                _syncStore.Add(fileId, null, null, triggeredAt, DateTime.Now, "SKIPPED",
                    error: "No valid VERSION_ID in VERSION_FILE_MAP", table: "FILES");
                return;
            }

            var versions = await _db.Versions.AsNoTracking()
                .Where(v => versionIds.Contains(v.Id))
                .ToListAsync();

            _logger.LogInformation(
                "[FILE-SYNC STEP2] VERSION_FILE_MAP → VERSION | Queried IDs: [{Ids}] | Found {Count} record(s) | ATTACH_IDs: [{AttachIds}] | VERSION_NUMs: [{VerNums}]",
                string.Join(", ", versionIds), versions.Count,
                string.Join(", ", versions.Select(v => v.AttachId?.ToString() ?? "NULL")),
                string.Join(", ", versions.Select(v => v.VersionNum?.ToString() ?? "NULL")));

            if (versions.Count == 0)
            {
                _logger.LogWarning("[FILE-SYNC SKIP] FILES {FileId} | No VERSION records found for IDs: {Ids}",
                    fileId, string.Join(",", versionIds));
                _syncStore.Add(fileId, null, null, triggeredAt, DateTime.Now, "SKIPPED",
                    error: "No VERSION records found", table: "FILES");
                return;
            }

            // Step 3: VERSION.ATTACH_ID + VERSION.VERSION_NUM → ATTACHMENT_MAP.ATTACH_ID + ATTACHMENT_MAP.VERSION
            var attachIds = versions
                .Where(v => v.AttachId.HasValue)
                .Select(v => v.AttachId!.Value)
                .Distinct()
                .ToList();

            _logger.LogInformation(
                "[FILE-SYNC STEP3] VERSION → ATTACHMENT_MAP | ATTACH_IDs to query: [{AttachIds}]",
                string.Join(", ", attachIds));

            int? itemId = null;
            if (attachIds.Count > 0)
            {
                var attachmentMaps = await _db.AttachmentMaps.AsNoTracking()
                    .Where(am => am.AttachId != null && attachIds.Contains(am.AttachId.Value))
                    .ToListAsync();

                _logger.LogInformation(
                    "[FILE-SYNC STEP3] ATTACHMENT_MAP query | Found {Count} record(s) | Details: [{Details}]",
                    attachmentMaps.Count,
                    string.Join("; ", attachmentMaps.Select(am =>
                        $"ATTACH_ID={am.AttachId}, VERSION={am.Version}, PARENT_ID={am.ParentId?.ToString() ?? "NULL"}")));

                foreach (var ver in versions)
                {
                    if (!ver.AttachId.HasValue || !ver.VersionNum.HasValue)
                    {
                        _logger.LogInformation(
                            "[FILE-SYNC STEP3] Skipping VERSION {VerId}: ATTACH_ID={AttachId}, VERSION_NUM={VerNum} (null values)",
                            ver.Id, ver.AttachId?.ToString() ?? "NULL", ver.VersionNum?.ToString() ?? "NULL");
                        continue;
                    }

                    var match = attachmentMaps.FirstOrDefault(am =>
                        am.AttachId == ver.AttachId.Value && am.Version == ver.VersionNum.Value);

                    if (match is not null)
                    {
                        _logger.LogInformation(
                            "[FILE-SYNC STEP3] MATCH found | VERSION(ATTACH_ID={AttachId}, VERSION_NUM={VerNum}) → ATTACHMENT_MAP(ID={MapId}, PARENT_ID={ParentId})",
                            ver.AttachId.Value, ver.VersionNum.Value, match.Id, match.ParentId?.ToString() ?? "NULL");

                        if (match.ParentId.HasValue && match.ParentId.Value > 0)
                        {
                            itemId = match.ParentId;
                            break;
                        }

                        _logger.LogWarning(
                            "[FILE-SYNC STEP3] PARENT_ID={ParentId} is invalid (0 or null), skipping this match",
                            match.ParentId?.ToString() ?? "NULL");
                    }
                    else
                    {
                        _logger.LogInformation(
                            "[FILE-SYNC STEP3] No ATTACHMENT_MAP match for VERSION(ATTACH_ID={AttachId}, VERSION_NUM={VerNum})",
                            ver.AttachId.Value, ver.VersionNum.Value);
                    }
                }
            }
            else
            {
                _logger.LogWarning("[FILE-SYNC STEP3] FILES {FileId} | No ATTACH_IDs found in VERSION records", fileId);
            }

            // Step 4: ATTACHMENT_MAP.PARENT_ID → ITEM.ID → ITEM.ITEM_NUMBER
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

            // Step 5: Get file metadata from FILES and FILE_INFO
            var fileRecord = await _db.Files.AsNoTracking()
                .FirstOrDefaultAsync(f => f.Id == fileId);

            var fileInfo = await _db.FileInfos.AsNoTracking()
                .FirstOrDefaultAsync(fi => fi.FileId == fileId);

            var fileName = fileRecord?.FileName ?? $"file_{fileId}";
            var relativePath = fileInfo?.IfsFilepath ?? string.Empty;

            _logger.LogInformation(
                "[FILE-SYNC STEP5] FILES {FileId} | FileName: {FileName} | IFS_FILEPATH: {Path} | Resolved Item: {ItemNumber}",
                fileId, fileName, relativePath, string.IsNullOrWhiteSpace(itemNumber) ? "(EMPTY)" : itemNumber);

            _logger.LogInformation(
                "[FILE-SYNC CHAIN] FILES(ID={FileId}) → VERSION_FILE_MAP(VERSION_IDs=[{VerIds}]) → VERSION(ATTACH_IDs=[{AttachIds}], VERSION_NUMs=[{VerNums}]) → ATTACHMENT_MAP(PARENT_ID={ParentId}) → ITEM(ID={ItemId}, ITEM_NUMBER={ItemNumber}) | FILENAME={FileName} | IFS_FILEPATH={Path}",
                fileId,
                string.Join(",", versionFileMaps.Select(v => v.VersionId?.ToString() ?? "NULL")),
                string.Join(",", versions.Select(v => v.AttachId?.ToString() ?? "NULL")),
                string.Join(",", versions.Select(v => v.VersionNum?.ToString() ?? "NULL")),
                itemId?.ToString() ?? "NULL",
                itemId?.ToString() ?? "NULL",
                string.IsNullOrWhiteSpace(itemNumber) ? "(UNRESOLVED)" : itemNumber,
                fileName,
                string.IsNullOrWhiteSpace(relativePath) ? "(EMPTY)" : relativePath);

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
                        inventroyCode = itemNumber,
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
