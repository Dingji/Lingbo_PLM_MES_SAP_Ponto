using FileSyncService.Models.Dto;

using System.Text;

namespace FileSyncService.Services;

public sealed class FileSyncFileLogger
{
    private readonly string _basePath;
    private readonly ILogger<FileSyncFileLogger> _logger;

    public FileSyncFileLogger(IConfiguration configuration, ILogger<FileSyncFileLogger> logger)
    {
        _logger = logger;
        _basePath = Path.Combine(AppContext.BaseDirectory, "log", "requests");
        _logger.LogInformation("[FILE LOGGER] Initialized | Log directory: {BasePath}", _basePath);
    }

    public string WriteRequestLog(
        string requestId,
        DateTime receivedTime,
        string clientIp,
        FileSyncRequest request,
        FileSyncResponse response)
    {
        if (!Directory.Exists(_basePath))
            Directory.CreateDirectory(_basePath);

        var timestamp = receivedTime.ToLocalTime().ToString("yyyyMMdd_HHmmss");
        var fileName = $"REQ_{requestId}_{timestamp}.log";
        var filePath = Path.Combine(_basePath, fileName);

        var sb = new StringBuilder();
        sb.AppendLine("================================================================================");
        sb.AppendLine("  FileSyncService - Detailed Request Log");
        sb.AppendLine($"  Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}");
        sb.AppendLine("================================================================================");
        sb.AppendLine($"  Request ID:     {requestId}");
        sb.AppendLine($"  Received At:    {receivedTime.ToLocalTime():yyyy-MM-dd HH:mm:ss.fff}");
        sb.AppendLine($"  Completed At:   {response.CompletedTimeUtc.ToLocalTime():yyyy-MM-dd HH:mm:ss.fff}");
        sb.AppendLine($"  Duration:       {response.DurationMs}ms");
        sb.AppendLine($"  Client IP:      {clientIp}");
        sb.AppendLine($"  Status:         {response.Status}");
        sb.AppendLine($"  Total Files:    {response.TotalFiles}");
        sb.AppendLine($"  Success:        {response.SuccessCount}");
        sb.AppendLine($"  Failed:         {response.FailedCount}");
        if (!string.IsNullOrEmpty(response.ErrorMessage))
            sb.AppendLine($"  Error:          {response.ErrorMessage}");
        sb.AppendLine("================================================================================");
        sb.AppendLine();

        sb.AppendLine("--- Received Request Body ---");
        sb.AppendLine();
        if (request.Files is { Count: > 0 })
        {
            for (int i = 0; i < request.Files.Count; i++)
            {
                var f = request.Files[i];
                sb.AppendLine($"  [{i + 1}] InventoryCode:  {f.InventoryCode ?? "(null)"}");
                sb.AppendLine($"      FileCode:       {f.FileCode ?? "(null)"}");
                sb.AppendLine($"      FileName:       {f.FileName ?? "(null)"}");
                sb.AppendLine($"      Remark:         {f.Remark ?? "(null)"}");
                sb.AppendLine($"      FileUrl:        {f.FileUrl ?? "(null)"}");
                sb.AppendLine($"      RelativePath:   {f.RelativePath ?? "(null)"}");
                sb.AppendLine();
            }
        }
        else
        {
            sb.AppendLine("  (empty or null files array)");
            sb.AppendLine();
        }

        sb.AppendLine("================================================================================");
        sb.AppendLine("  Per-File Processing Details");
        sb.AppendLine("================================================================================");
        sb.AppendLine();

        if (response.Results is { Count: > 0 })
        {
            for (int i = 0; i < response.Results.Count; i++)
            {
                var r = response.Results[i];
                sb.AppendLine($"  +++++ File [{i + 1}]: {r.FileName ?? "unknown"} +++++");
                sb.AppendLine($"  InventoryCode:      {r.InventoryCode ?? "-"}");
                sb.AppendLine($"  FileCode:           {r.FileCode ?? "-"}");
                sb.AppendLine($"  FileName:           {r.FileName ?? "-"}");
                sb.AppendLine($"  Result:             {(r.Success ? "SUCCESS" : "FAILED")}");
                if (!r.Success)
                    sb.AppendLine($"  ErrorMessage:       {r.ErrorMessage ?? "-"}");
                sb.AppendLine();

                if (r.Steps.Count > 0)
                {
                    sb.AppendLine($"  Processing Timeline:");
                    sb.AppendLine($"  {new string('-', 120)}");
                    sb.AppendLine($"  {"Time (UTC)",-25} {"Step",-28} {"Detail",-60}");
                    sb.AppendLine($"  {new string('-', 120)}");

                    var firstTime = r.Steps[0].TimestampUtc;
                    foreach (var step in r.Steps)
                    {
                        var elapsed = (step.TimestampUtc - firstTime).TotalMilliseconds;
                        sb.AppendLine(
                            $"  {step.TimestampUtc:yyyy-MM-dd HH:mm:ss.fff}  {step.Step,-28} {Truncate(step.Detail, 58),-60} [{elapsed,+8:F0}ms]");
                    }
                    sb.AppendLine($"  {new string('-', 120)}");
                    sb.AppendLine();
                }

                if (r.Success)
                {
                    sb.AppendLine($"  File Info:");
                    sb.AppendLine($"    Size (bytes):       {r.SizeBytes}");
                    sb.AppendLine($"    Size (human):       {FormatSize(r.SizeBytes)}");
                    sb.AppendLine($"    MD5:                {r.Md5 ?? "-"}");
                    sb.AppendLine($"    Created (UTC):      {r.CreationTimeUtc:yyyy-MM-dd HH:mm:ss}");
                    sb.AppendLine($"    LastModified (UTC): {r.LastModifiedTimeUtc:yyyy-MM-dd HH:mm:ss}");
                    sb.AppendLine($"    Full Path:          {r.FullPath ?? "-"}");
                    sb.AppendLine($"    Relative Path:      {r.RelativePath ?? "-"}");
                    sb.AppendLine($"    Is Zip File:        {(r.IsZipFile ? "YES" : "NO")}");

                    if (r.IsZipFile && r.ZipEntries is { Count: > 0 })
                    {
                        sb.AppendLine();
                        sb.AppendLine($"  ZIP Archive Contents ({r.ZipEntries.Count} entries):");
                        sb.AppendLine($"  {new string('-', 140)}");
                        sb.AppendLine($"    {"Entry Name",-55} {"Size",10} {"Compressed",10} {"MD5",-34} {"Modified (UTC)"}");
                        sb.AppendLine($"  {new string('-', 140)}");
                        foreach (var entry in r.ZipEntries)
                        {
                            var name = entry.IsDirectory ? entry.EntryName + "/" : entry.EntryName;
                            var md5 = entry.Md5 ?? "-";
                            if (entry.IsDirectory) md5 = "-";
                            sb.AppendLine(
                                $"    {Truncate(name, 53),-55} {entry.UncompressedSizeBytes,10} {entry.CompressedSizeBytes,10} {md5,-34} {entry.LastModifiedTimeUtc:yyyy-MM-dd HH:mm:ss}");
                        }
                        sb.AppendLine($"  {new string('-', 140)}");
                    }
                }
                sb.AppendLine();
            }
        }
        else
        {
            sb.AppendLine("  (no results)");
            sb.AppendLine();
        }

        sb.AppendLine("================================================================================");
        sb.AppendLine($"  End of Request Log | Status: {response.Status} | Duration: {response.DurationMs}ms");
        sb.AppendLine("================================================================================");

        File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);

        _logger.LogInformation(
            "[FILE WRITE] Request log written | File: {FileName} | Size: {Size} bytes",
            fileName, new FileInfo(filePath).Length);

        return filePath;
    }

    public void AppendMesSection(
        string logFilePath,
        string endpoint,
        string requestJson,
        int? httpStatus,
        string? responseBody,
        string? error,
        DateTime requestTime,
        DateTime responseTime)
    {
        if (!File.Exists(logFilePath))
        {
            _logger.LogWarning("[FILE APPEND] Log file not found for MES section append: {Path}", logFilePath);
            return;
        }

        var sb = new StringBuilder();
        sb.AppendLine();
        sb.AppendLine("================================================================================");
        sb.AppendLine("  MES File Upload Log");
        sb.AppendLine("================================================================================");
        sb.AppendLine($"  Endpoint:       {endpoint}");
        sb.AppendLine($"  Request Time:   {requestTime:yyyy-MM-dd HH:mm:ss.fff}");
        sb.AppendLine($"  Response Time:  {responseTime:yyyy-MM-dd HH:mm:ss.fff}");
        sb.AppendLine($"  Duration:       {(responseTime - requestTime).TotalMilliseconds:F0}ms");
        sb.AppendLine($"  HTTP Status:    {(httpStatus.HasValue ? httpStatus.Value.ToString() : "N/A (exception)")}");
        sb.AppendLine();
        sb.AppendLine("--- Request Body (file_content omitted) ---");
        sb.AppendLine(SanitizeBase64Content(requestJson));
        sb.AppendLine();
        sb.AppendLine("--- Response Body ---");
        if (error is not null)
            sb.AppendLine($"[ERROR] {error}");
        else
            sb.AppendLine(responseBody ?? "(empty)");
        sb.AppendLine();
        sb.AppendLine("================================================================================");

        File.AppendAllText(logFilePath, sb.ToString(), Encoding.UTF8);

        var info = new FileInfo(logFilePath);
        _logger.LogInformation(
            "[FILE APPEND] MES section appended | File: {File} | Size: {Size} bytes",
            info.Name, info.Length);
    }

    public IReadOnlyList<string> ListLogFiles()
    {
        if (!Directory.Exists(_basePath))
            return Array.Empty<string>();

        return Directory.GetFiles(_basePath, "*.log")
            .Select(Path.GetFileName)
            .Where(f => f != null)
            .Cast<string>()
            .OrderByDescending(f => f)
            .Take(200)
            .ToList();
    }

    public string? ReadLogFile(string fileName)
    {
        var safeName = Path.GetFileName(fileName);
        var filePath = Path.Combine(_basePath, safeName);

        if (!File.Exists(filePath))
            return null;

        return File.ReadAllText(filePath, Encoding.UTF8);
    }

    private static string SanitizeBase64Content(string json)
    {
        return System.Text.RegularExpressions.Regex.Replace(
            json,
            "\"file_content\"\\s*:\\s*\"([^\"]*)\"",
            m => $"\"file_content\":\"[BASE64 {m.Groups[1].Value.Length} chars omitted]\"");
    }

    private static string FormatSize(long bytes)
    {
        if (bytes < 1024) return $"{bytes} B";
        if (bytes < 1024 * 1024) return $"{bytes / 1024.0:F1} KB";
        if (bytes < 1024L * 1024 * 1024) return $"{bytes / (1024.0 * 1024):F1} MB";
        return $"{bytes / (1024.0 * 1024 * 1024):F2} GB";
    }

    private static string Truncate(string? value, int maxLen)
    {
        if (string.IsNullOrEmpty(value)) return "-";
        return value.Length <= maxLen ? value : value[..(maxLen - 2)] + "..";
    }
}
