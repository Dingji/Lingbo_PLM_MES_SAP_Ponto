using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

using Microsoft.Extensions.Logging;

namespace FileSyncService.Services;

public sealed class LogReaderService
{
    private readonly string _logDir;
    private readonly ILogger<LogReaderService> _logger;

    public LogReaderService(ILogger<LogReaderService> logger)
    {
        _logger = logger;
        _logDir = Path.Combine(AppContext.BaseDirectory, "log", "requests");
        _logger.LogInformation("[LOG READER] Initialized | Log directory: {LogDir}", _logDir);
    }

    public IReadOnlyList<string> ListLogFiles()
    {
        if (!Directory.Exists(_logDir))
            return Array.Empty<string>();

        return Directory.GetFiles(_logDir, "*.log")
            .Select(Path.GetFileName)
            .Where(f => f != null)
            .Cast<string>()
            .OrderByDescending(f => f)
            .Take(200)
            .ToList();
    }

    private static readonly Regex FileContentRegex = new(
        @"""file_content"":\s*""[^""]{100,}""",
        RegexOptions.Compiled);

    public string? ReadLogFile(string fileName)
    {
        var safeName = Path.GetFileName(fileName);
        var filePath = Path.Combine(_logDir, safeName);

        if (!File.Exists(filePath))
        {
            _logger.LogWarning("[LOG READER] File not found: {FilePath}", filePath);
            return null;
        }

        var content = ReadFileShared(filePath);
        // Strip base64 file content to reduce transfer size.
        content = FileContentRegex.Replace(content, @"""file_content"": ""[base64]""");
        return content;
    }

    private static string ReadFileShared(string filePath)
    {
        using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var reader = new StreamReader(fs);
        return reader.ReadToEnd();
    }
}
