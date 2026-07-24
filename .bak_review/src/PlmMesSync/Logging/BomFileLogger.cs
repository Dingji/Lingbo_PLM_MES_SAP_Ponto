using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using PlmMesSync.Models.Dto;

namespace PlmMesSync.Logging;

public class BomFileLogger
{
    private readonly string _basePath;
    private readonly ILogger<BomFileLogger> _logger;

    public BomFileLogger(IConfiguration configuration, ILogger<BomFileLogger> logger)
    {
        _logger = logger;
        var logBase = configuration["SyncSettings:LogBasePath"] ?? "log";
        _basePath = Path.Combine(AppContext.BaseDirectory, logBase, "bom");
        _logger.LogInformation("[FILE LOGGER] Initialized | Log directory: {BasePath}", _basePath);
    }

    public string WriteSyncLog(BomSyncOutput output)
    {
        if (!Directory.Exists(_basePath))
        {
            Directory.CreateDirectory(_basePath);
            _logger.LogInformation("[FILE LOGGER] Created log directory: {BasePath}", _basePath);
        }

        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var safeItemNumber = string.Join("_", output.ParentItemNumber.Split(Path.GetInvalidFileNameChars()));
        var fileName = $"BOM_{safeItemNumber}_{timestamp}.log";
        var filePath = Path.Combine(_basePath, fileName);

        _logger.LogInformation(
            "[FILE WRITE] Writing sync log | File: {FileName} | Item: {ItemNumber} | Versions: {VersionCount}",
            fileName, output.ParentItemNumber, output.Versions.Count);

        var sb = new StringBuilder();
        sb.AppendLine("================================================================");
        sb.AppendLine("  BOM Sync Report (Latest Version Only)");
        sb.AppendLine($"  Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine("================================================================");
        sb.AppendLine($"  Parent Item:    {output.ParentItemNumber}");
        sb.AppendLine($"  Description:    {output.ParentDescription ?? "-"}");
        sb.AppendLine("================================================================");
        sb.AppendLine();

        var latestVersion = output.Versions
            .Where(v => v.IsLatest)
            .OrderByDescending(v => v.ReleaseDate ?? DateTime.MinValue)
            .FirstOrDefault() ?? output.Versions.LastOrDefault();

        if (latestVersion is not null)
        {
            var releaseStr = latestVersion.ReleaseDate?.ToString("yyyy-MM-dd") ?? "N/A";
            sb.AppendLine($"--- Version: {latestVersion.RevNumber} [LATEST] | Released: {releaseStr} | Lines: {latestVersion.Lines.Count} ---");
            sb.AppendLine();

            if (latestVersion.Lines.Count == 0)
            {
                sb.AppendLine("  (No active BOM lines in this version)");
                sb.AppendLine();
            }
            else
            {
                sb.AppendLine($"  {"Lvl",-4} {"Find#",6} | {"Item Number",-20} | {"Qty",-6} | {"Rev",-6} | {"Description",-28} | {"RefDesig",-18} | {"SubGrp",-8} | {"SubPri",-6} | {"ChgIn",-12} | {"ChgOut",-12}");
                sb.AppendLine($"  {new string('-', 145)}");

                foreach (var line in latestVersion.Lines)
                {
                    var indent = new string(' ', (line.Level - 1) * 4);
                    var treeMark = line.HasChildren ? "+" : " ";
                    var levelStr = $"{line.Level}{treeMark}";

                    sb.AppendLine(
                        $"  {levelStr,-4} {indent}{line.FindNumber,-6} | {line.ItemNumber ?? "-",20} | {line.Quantity ?? "-",6} | " +
                        $"{line.ComponentRev ?? "-",6} | {Truncate(line.Description, 28),-28} | {Truncate(line.RefDesig, 18),-18} | " +
                        $"{line.SubstituteGroup ?? "-",8} | {line.SubstitutePriority ?? "-",6} | " +
                        $"{line.ChangeInNumber ?? "-",12} | {line.ChangeOutNumber ?? "-",12}");
                }

                sb.AppendLine();
            }
        }
        else
        {
            sb.AppendLine("  (No versions found)");
            sb.AppendLine();
        }

        sb.AppendLine("================================================================");
        sb.AppendLine($"  End of Report | Latest version lines: {latestVersion?.Lines.Count ?? 0}");
        sb.AppendLine("================================================================");

        File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);

        var fileSize = new FileInfo(filePath).Length;
        _logger.LogInformation(
            "[FILE WRITE DONE] Log file written | Path: {FilePath} | Size: {FileSize} bytes",
            filePath, fileSize);

        return filePath;
    }

    private static string Truncate(string? value, int maxLen)
    {
        if (string.IsNullOrEmpty(value)) return "-";
        return value.Length <= maxLen ? value : value[..(maxLen - 2)] + "..";
    }

    public IReadOnlyList<string> ListLogFiles()
    {
        if (!Directory.Exists(_basePath))
            return Array.Empty<string>();

        var files = Directory.GetFiles(_basePath, "*.log")
            .Select(Path.GetFileName)
            .Where(f => f != null)
            .Cast<string>()
            .OrderByDescending(f => f)
            .Take(100)
            .ToList();

        _logger.LogDebug("[FILE LIST] Listed {Count} log files from {BasePath}", files.Count, _basePath);
        return files;
    }

    public string? ReadLogFile(string fileName)
    {
        var safeName = Path.GetFileName(fileName);
        var filePath = Path.Combine(_basePath, safeName);

        if (!File.Exists(filePath))
        {
            _logger.LogWarning("[FILE READ] File not found: {FilePath}", filePath);
            return null;
        }

        _logger.LogDebug("[FILE READ] Reading log file: {FileName}", safeName);
        return File.ReadAllText(filePath, Encoding.UTF8);
    }

    public void AppendMesSection(string logFilePath, string endpoint, string requestJson,
        int? httpStatus, string? responseBody, string? error, DateTime requestTime, DateTime responseTime)
    {
        if (!File.Exists(logFilePath))
            return;

        var sb = new StringBuilder();
        sb.AppendLine();
        sb.AppendLine("================================================================");
        sb.AppendLine("  MES Upload Log");
        sb.AppendLine("================================================================");
        sb.AppendLine($"  Endpoint:       {endpoint}");
        sb.AppendLine($"  Request Time:   {requestTime:yyyy-MM-dd HH:mm:ss.fff}");
        sb.AppendLine($"  Response Time:  {responseTime:yyyy-MM-dd HH:mm:ss.fff}");
        sb.AppendLine($"  Duration:       {(responseTime - requestTime).TotalMilliseconds:F0}ms");
        sb.AppendLine($"  HTTP Status:    {(httpStatus.HasValue ? httpStatus.Value.ToString() : "N/A (exception)")}");
        sb.AppendLine();
        sb.AppendLine("--- Request Body ---");
        sb.AppendLine(requestJson);
        sb.AppendLine();
        sb.AppendLine("--- Response Body ---");
        if (error is not null)
            sb.AppendLine($"[ERROR] {error}");
        else
            sb.AppendLine(responseBody ?? "(empty)");
        sb.AppendLine();
        sb.AppendLine("================================================================");

        File.AppendAllText(logFilePath, sb.ToString(), Encoding.UTF8);
        _logger.LogDebug("[FILE APPEND] MES section appended to {Path}", logFilePath);
    }
}
