using Microsoft.Extensions.Options;

using FileSyncService.Models.Dto;

using SharpCompress.Archives;
using SharpCompress.Readers;

using System.Security.Cryptography;

namespace FileSyncService.Services;

/// <summary>
/// Configuration options for the file sync service.
/// </summary>
public sealed class FileSyncOptions
{
    /// <summary>
    /// The configuration section name.
    /// </summary>
    public const string SectionName = "FileSyncSettings";

    /// <summary>
    /// The root directory where downloaded files are stored.
    /// </summary>
    public string RootDirectory { get; set; } = "D:\\FileSyncRoot";

    /// <summary>
    /// Timeout in seconds for each file download.
    /// </summary>
    public int DownloadTimeoutSeconds { get; set; } = 120;

    /// <summary>
    /// Maximum number of concurrent file downloads.
    /// </summary>
    public int MaxConcurrentDownloads { get; set; } = 10;

    /// <summary>
    /// Maximum allowed file size in bytes (default 512 MB).
    /// </summary>
    public long MaxFileSizeBytes { get; set; } = 536870912;
}

/// <summary>
/// Service responsible for downloading files from URLs and analyzing their content.
/// Handles ZIP file extraction in memory and computes file metadata including MD5 hashes.
/// </summary>
public sealed class FileSyncProcessor
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly FileSyncOptions _options;
    private readonly ILogger<FileSyncProcessor> _logger;
    private readonly SemaphoreSlim _downloadSemaphore;

    public FileSyncProcessor(
        IHttpClientFactory httpClientFactory,
        IOptions<FileSyncOptions> options,
        ILogger<FileSyncProcessor> logger)
    {
        _httpClientFactory = httpClientFactory;
        _options = options.Value;
        _logger = logger;
        _downloadSemaphore = new SemaphoreSlim(_options.MaxConcurrentDownloads, _options.MaxConcurrentDownloads);
    }

    public async Task<FileInfoResult> ProcessFileAsync(FileSyncItem item, CancellationToken cancellationToken = default)
    {
        var result = new FileInfoResult
        {
            InventroyCode = item.InventroyCode,
            FileCode = item.FileCode,
            FileName = item.FileName,
            Remark = item.Remark
        };

        result.AddStep("REQUEST_RECEIVED", $"FileCode={item.FileCode}, InventroyCode={item.InventroyCode}, FileName={item.FileName}");

        _logger.LogInformation(
            "[FileSync][RECEIVED] FileCode={FileCode} | InventroyCode={InventroyCode} | FileName={FileName} | Remark={Remark} | FileUrl={FileUrl} | RelativePath={RelativePath}",
            item.FileCode ?? "(null)", item.InventroyCode ?? "(null)", item.FileName ?? "(null)",
            item.Remark ?? "(null)", item.FileUrl ?? "(null)", item.RelativePath ?? "(null)");

        try
        {
            string fullPath;
            string relativePath;

            if (!string.IsNullOrWhiteSpace(item.RelativePath))
            {
                result.AddStep("MODE_LOCAL_FILE", $"RelativePath={item.RelativePath}");

                relativePath = item.RelativePath.Replace("\\", "/").TrimStart('/');
                relativePath = relativePath.Replace("..", "");
                fullPath = Path.GetFullPath(Path.Combine(_options.RootDirectory, relativePath));

                result.AddStep("PATH_RESOLVED", $"RelativePath={relativePath}, FullPath={fullPath}, Root={_options.RootDirectory}");

                if (!fullPath.StartsWith(Path.GetFullPath(_options.RootDirectory), StringComparison.OrdinalIgnoreCase))
                {
                    result.Success = false;
                    result.ErrorMessage = "Relative path resolves outside the root directory.";
                    result.AddStep("SECURITY_BLOCKED", $"Path traversal detected: {item.RelativePath} -> {fullPath}");
                    _logger.LogWarning("[FileSync][SECURITY] FileCode={FileCode} | PATH TRAVERSAL DETECTED | Input: {Input} | Resolved: {Resolved} | Root: {Root}", item.FileCode, item.RelativePath, fullPath, _options.RootDirectory);
                    return result;
                }

                result.RelativePath = relativePath;
                result.FullPath = fullPath;

                if (!File.Exists(fullPath))
                {
                    result.Success = false;
                    result.ErrorMessage = $"Local file not found: {fullPath}";
                    result.AddStep("FILE_NOT_FOUND", $"Local file does not exist: {fullPath}");
                    _logger.LogWarning("[FileSync][NOT FOUND] FileCode={FileCode} | Local file does not exist | Path: {Path}", item.FileCode, fullPath);
                    return result;
                }

                result.AddStep("FILE_FOUND", $"Local file exists: {fullPath}");
            }
            else
            {
                if (string.IsNullOrWhiteSpace(item.FileUrl))
                {
                    result.Success = false;
                    result.ErrorMessage = "Both file_url and relative_path are empty.";
                    result.AddStep("ERROR", "Both FileUrl and RelativePath are empty");
                    _logger.LogWarning("[FileSync][ERROR] FileCode={FileCode} | Both FileUrl and RelativePath are empty", item.FileCode);
                    return result;
                }

                if (!Uri.TryCreate(item.FileUrl, UriKind.Absolute, out var uri))
                {
                    result.Success = false;
                    result.ErrorMessage = $"Invalid file_url format: {item.FileUrl}";
                    result.AddStep("ERROR", $"Invalid URL format: {item.FileUrl}");
                    _logger.LogWarning("[FileSync][ERROR] FileCode={FileCode} | Invalid URL format: {Url}", item.FileCode, item.FileUrl);
                    return result;
                }

                result.AddStep("MODE_DOWNLOAD", $"URL={item.FileUrl}");

                relativePath = BuildRelativePath(item);
                fullPath = Path.Combine(_options.RootDirectory, relativePath);
                result.RelativePath = relativePath;
                result.FullPath = fullPath;

                result.AddStep("PATH_SET", $"RelativePath={relativePath}, FullPath={fullPath}");

                var directory = Path.GetDirectoryName(fullPath);
                if (!string.IsNullOrEmpty(directory))
                {
                    Directory.CreateDirectory(directory);
                    result.AddStep("DIR_CREATED", $"Ensured directory exists: {directory}");
                }

                result.AddStep("DOWNLOAD_WAITING", $"Waiting for download semaphore (max concurrent: {_options.MaxConcurrentDownloads})");

                await _downloadSemaphore.WaitAsync(cancellationToken);
                try
                {
                    result.AddStep("DOWNLOAD_START", $"Starting download from {uri}");
                    var downloadStart = DateTime.UtcNow;
                    await DownloadFileAsync(uri, fullPath, cancellationToken);
                    var downloadDuration = (DateTime.UtcNow - downloadStart).TotalMilliseconds;
                    result.AddStep("DOWNLOAD_COMPLETE", $"Download completed in {downloadDuration:F0}ms, saved to {fullPath}");
                }
                finally
                {
                    _downloadSemaphore.Release();
                }

                var fileExist = File.Exists(fullPath);
                result.AddStep("DOWNLOAD_VERIFIED", $"File exists after download: {fileExist}");

                if (!fileExist)
                {
                    result.Success = false;
                    result.ErrorMessage = "File not found after download. The download may have failed silently.";
                    result.AddStep("ERROR", "File not found after download");
                    _logger.LogError("[FileSync][ERROR] FileCode={FileCode} | File not found after download: {Path}", item.FileCode, fullPath);
                    return result;
                }
            }

            result.AddStep("ANALYSIS_START", "Starting file analysis");

            var fileInfo = new FileInfo(fullPath);
            result.SizeBytes = fileInfo.Length;
            result.CreationTimeUtc = fileInfo.CreationTimeUtc;
            result.LastModifiedTimeUtc = fileInfo.LastWriteTimeUtc;

            result.AddStep("FILE_INFO", $"Size={fileInfo.Length} bytes, Created={fileInfo.CreationTimeUtc:yyyy-MM-dd HH:mm:ss} UTC, Modified={fileInfo.LastWriteTimeUtc:yyyy-MM-dd HH:mm:ss} UTC, Extension={fileInfo.Extension}");

            result.AddStep("MD5_START", "Computing MD5 hash");
            result.Md5 = await ComputeFileMd5Async(fullPath, cancellationToken);
            result.AddStep("MD5_COMPLETE", $"MD5={result.Md5}");

            var archiveType = DetectArchiveType(item.FileName ?? "", fullPath);
            result.AddStep("ARCHIVE_CHECK", $"Archive detection: {(archiveType ?? "NONE")} (FileName: {item.FileName})");

            if (archiveType is not null)
            {
                result.IsArchiveFile = true;
                result.ArchiveType = archiveType;

                if (archiveType == "ZIP")
                    result.IsZipFile = true;

                result.AddStep("ARCHIVE_EXTRACT_START", $"Extracting {archiveType} entries");
                result.ZipEntries = await ExtractArchiveEntriesAsync(fullPath, archiveType, cancellationToken);
                result.AddStep("ARCHIVE_EXTRACT_COMPLETE", $"Extracted {result.ZipEntries.Count} entries from {archiveType}");
            }

            result.Success = true;
            result.AddStep("SUCCESS", $"FileName={item.FileName}, Size={result.SizeBytes} bytes, MD5={result.Md5}, IsZip={result.IsZipFile}");
        }
        catch (OperationCanceledException)
        {
            result.Success = false;
            result.ErrorMessage = "Operation was cancelled.";
            result.AddStep("CANCELLED", "Processing was cancelled");
            _logger.LogWarning("[FileSync][CANCELLED] FileCode={FileCode} | Processing was cancelled", item.FileCode);
        }
        catch (HttpRequestException ex)
        {
            result.Success = false;
            result.ErrorMessage = $"Download failed: {ex.Message}";
            result.AddStep("HTTP_ERROR", $"URL={item.FileUrl}, Error={ex.Message}");
            _logger.LogError(ex, "[FileSync][HTTP ERROR] FileCode={FileCode} | URL: {Url} | Error: {Error}", item.FileCode, item.FileUrl, ex.Message);
        }
        catch (TimeoutException ex)
        {
            result.Success = false;
            result.ErrorMessage = $"Download timed out: {ex.Message}";
            result.AddStep("TIMEOUT", $"Error={ex.Message}");
            _logger.LogError(ex, "[FileSync][TIMEOUT] FileCode={FileCode} | Error: {Error}", item.FileCode, ex.Message);
        }
        catch (IOException ex)
        {
            result.Success = false;
            result.ErrorMessage = $"File I/O error: {ex.Message}";
            result.AddStep("IO_ERROR", $"Error={ex.Message}");
            _logger.LogError(ex, "[FileSync][IO ERROR] FileCode={FileCode} | Error: {Error}", item.FileCode, ex.Message);
        }
        catch (InvalidDataException ex)
        {
            result.Success = false;
            result.ErrorMessage = $"ZIP file is corrupted or invalid: {ex.Message}";
            result.AddStep("ZIP_ERROR", $"Error={ex.Message}");
            _logger.LogError(ex, "[FileSync][ZIP ERROR] FileCode={FileCode} | Corrupted ZIP | Error: {Error}", item.FileCode, ex.Message);
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.ErrorMessage = $"Unexpected error: {ex.Message}";
            result.AddStep("UNEXPECTED_ERROR", $"Error={ex.Message}");
            _logger.LogError(ex, "[FileSync][UNEXPECTED] FileCode={FileCode} | Error: {Error}", item.FileCode, ex.Message);
        }

        return result;
    }

    /// <summary>
    /// Builds the relative path for storing the file under the root directory.
    /// Format: inventroyCode/fileName
    /// </summary>
    private static string BuildRelativePath(FileSyncItem item)
    {
        var folder = string.IsNullOrWhiteSpace(item.InventroyCode) ? "_default" : SanitizePath(item.InventroyCode);
        var fileName = string.IsNullOrWhiteSpace(item.FileName) ? "unnamed_file" : SanitizeFileName(item.FileName);
        return Path.Combine(folder, fileName);
    }

    /// <summary>
    /// Downloads a file from the specified URI to the local path.
    /// </summary>
    private async Task DownloadFileAsync(Uri uri, string destinationPath, CancellationToken cancellationToken)
    {
        var client = _httpClientFactory.CreateClient("FileSyncDownload");

        _logger.LogInformation("[FileSync] Downloading: {Url} -> {Path}", uri, destinationPath);

        using var response = await client.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();

        // Check content length against the maximum allowed size.
        var contentLength = response.Content.Headers.ContentLength;
        if (contentLength.HasValue && contentLength.Value > _options.MaxFileSizeBytes)
        {
            throw new IOException(
                $"File size {contentLength.Value} bytes exceeds the maximum allowed size of {_options.MaxFileSizeBytes} bytes.");
        }

        // Stream the content to disk to avoid loading large files entirely into memory.
        await using var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken);
        await using var fileStream = new FileStream(
            destinationPath, FileMode.Create, FileAccess.Write, FileShare.None, 81920, useAsync: true);

        await contentStream.CopyToAsync(fileStream, cancellationToken);

        _logger.LogInformation("[FileSync] Download completed: {Path}", destinationPath);
    }

    /// <summary>
    /// Computes the MD5 hash of a file.
    /// </summary>
    private static async Task<string> ComputeFileMd5Async(string filePath, CancellationToken cancellationToken)
    {
        await using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, useAsync: true);
        var hashBytes = await MD5.HashDataAsync(stream, cancellationToken);
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }

    private static readonly string[] ArchiveExtensions =
        [".zip", ".rar", ".7z", ".tar", ".gz", ".tgz", ".bz2", ".tar.gz", ".tar.bz2", ".xz", ".tar.xz"];

    /// <summary>
    /// Detects the archive type by file extension and magic bytes.
    /// Returns a type string (ZIP, RAR, 7Z, TAR, GZIP, BZIP2, XZ) or null if not an archive.
    /// </summary>
    private static string? DetectArchiveType(string fileName, string fullPath)
    {
        var lowerName = fileName.ToLowerInvariant();

        if (lowerName.EndsWith(".tar.gz") || lowerName.EndsWith(".tgz"))
            return "TAR.GZ";
        if (lowerName.EndsWith(".tar.bz2"))
            return "TAR.BZ2";
        if (lowerName.EndsWith(".tar.xz"))
            return "TAR.XZ";
        if (lowerName.EndsWith(".zip"))
            return "ZIP";
        if (lowerName.EndsWith(".rar"))
            return "RAR";
        if (lowerName.EndsWith(".7z"))
            return "7Z";
        if (lowerName.EndsWith(".tar"))
            return "TAR";
        if (lowerName.EndsWith(".gz"))
            return "GZIP";
        if (lowerName.EndsWith(".bz2"))
            return "BZIP2";
        if (lowerName.EndsWith(".xz"))
            return "XZ";

        // Fallback: detect by magic bytes.
        try
        {
            using var fs = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read);
            if (fs.Length < 4) return null;

            Span<byte> magic = stackalloc byte[8];
            var bytesRead = fs.Read(magic);

            if (bytesRead >= 4 && magic[0] == 0x50 && magic[1] == 0x4B && magic[2] == 0x03 && magic[3] == 0x04)
                return "ZIP";
            if (bytesRead >= 7 && magic[0] == 0x52 && magic[1] == 0x61 && magic[2] == 0x72 && magic[3] == 0x21 && magic[4] == 0x1A && magic[5] == 0x07)
                return "RAR";
            if (bytesRead >= 6 && magic[0] == 0x37 && magic[1] == 0x7A && magic[2] == 0xBC && magic[3] == 0xAF && magic[4] == 0x27 && magic[5] == 0x1C)
                return "7Z";
            if (bytesRead >= 2 && magic[0] == 0x1F && magic[1] == 0x8B)
                return "GZIP";
            if (bytesRead >= 3 && magic[0] == 0x42 && magic[1] == 0x5A && magic[2] == 0x68)
                return "BZIP2";
            if (bytesRead >= 6 && magic[0] == 0xFD && magic[1] == 0x37 && magic[2] == 0x7A && magic[3] == 0x58 && magic[4] == 0x5A && magic[5] == 0x00)
                return "XZ";
        }
        catch
        {
            // Ignore read errors during detection.
        }

        return null;
    }

    /// <summary>
    /// Extracts archive entries using SharpCompress (supports ZIP, RAR, 7Z, TAR, GZ, BZ2, XZ, etc.).
    /// </summary>
    private async Task<List<ZipEntryInfo>> ExtractArchiveEntriesAsync(string archivePath, string archiveType, CancellationToken cancellationToken)
    {
        var entries = new List<ZipEntryInfo>();

        await Task.Run(() =>
        {
            using var archive = ArchiveFactory.OpenArchive(archivePath, new ReaderOptions());

            foreach (var entry in archive.Entries)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var isDirectory = entry.IsDirectory;

                var entryInfo = new ZipEntryInfo
                {
                    EntryName = entry.Key ?? "",
                    UncompressedSizeBytes = entry.Size,
                    CompressedSizeBytes = entry.CompressedSize,
                    LastModifiedTimeUtc = entry.LastModifiedTime ?? DateTime.MinValue,
                    IsDirectory = isDirectory
                };

                if (!isDirectory && entry.Size > 0)
                {
                    try
                    {
                        using var entryStream = entry.OpenEntryStream();
                        var hashBytes = MD5.HashData(entryStream);
                        entryInfo.Md5 = Convert.ToHexString(hashBytes).ToLowerInvariant();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "[FileSync] Failed to compute MD5 for archive entry: {Entry}", entry.Key);
                        entryInfo.Md5 = "ERROR";
                    }
                }
                else
                {
                    entryInfo.Md5 = string.Empty;
                }

                entries.Add(entryInfo);
            }
        }, cancellationToken);

        _logger.LogInformation("[FileSync] Extracted {Count} entries from {Type} archive: {Path}", entries.Count, archiveType, archivePath);
        return entries;
    }

    /// <summary>
    /// Sanitizes a path segment to prevent directory traversal attacks.
    /// </summary>
    private static string SanitizePath(string input)
    {
        // Remove any path traversal characters and invalid path chars.
        var sanitized = input.Replace("..", "").Replace("/", "_").Replace("\\", "_");
        var invalidChars = Path.GetInvalidPathChars();
        foreach (var c in invalidChars)
        {
            sanitized = sanitized.Replace(c, '_');
        }
        return string.IsNullOrWhiteSpace(sanitized) ? "_default" : sanitized;
    }

    /// <summary>
    /// Sanitizes a file name to prevent invalid characters and path traversal.
    /// </summary>
    private static string SanitizeFileName(string input)
    {
        var sanitized = input.Replace("..", "").Replace("/", "_").Replace("\\", "_");
        var invalidChars = Path.GetInvalidFileNameChars();
        foreach (var c in invalidChars)
        {
            sanitized = sanitized.Replace(c, '_');
        }
        return string.IsNullOrWhiteSpace(sanitized) ? "unnamed_file" : sanitized;
    }
}
