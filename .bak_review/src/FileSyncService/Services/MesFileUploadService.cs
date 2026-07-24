using FileSyncService.Models.Dto;

using Microsoft.Extensions.Options;

using SharpCompress.Archives;
using SharpCompress.Readers;

using System.Text;
using System.Text.Json;

namespace FileSyncService.Services;

public sealed class MesFileUploadOptions
{
    public const string SectionName = "MesUploadSettings";

    public string MesEndpoint { get; set; } = "http://10.170.9.17:8888/ims-integrate/api/updateImsData";

    public int TimeoutSeconds { get; set; } = 120;
}

public sealed class MesFileUploadService
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented = false
    };

    private readonly HttpClient _http;
    private readonly MesFileUploadOptions _options;
    private readonly ILogger<MesFileUploadService> _logger;

    public MesFileUploadService(
        HttpClient http,
        IOptions<MesFileUploadOptions> options,
        ILogger<MesFileUploadService> logger)
    {
        _http = http;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<List<MesUploadRecord>> UploadFilesToMes(List<FileInfoResult> results, CancellationToken cancellationToken = default)
    {
        var records = new List<MesUploadRecord>();

        var successResults = results.Where(r => r.Success).ToList();
        if (successResults.Count == 0)
        {
            _logger.LogWarning("[MES-FILE] No successful files to upload, skipping MES upload");
            return records;
        }

        var grouped = successResults.GroupBy(r => r.InventroyCode);

        foreach (var group in grouped)
        {
            var mtrlCode = group.Key;
            var fileList = new List<MesFileEntry>();
            var seq = 1;

            foreach (var result in group)
            {
                try
                {
                    if (result.IsArchiveFile)
                    {
                        var (archiveEntries, nextSeq) = await ReadArchiveEntriesAsBase64Async(result, seq, cancellationToken);
                        fileList.AddRange(archiveEntries);
                        seq = nextSeq;
                        _logger.LogInformation(
                            "[MES-FILE] FileCode={FileCode} | {ArchiveType} expanded into {Count} entries",
                            result.FileCode, result.ArchiveType, archiveEntries.Count);
                    }
                    else
                    {
                        var base64 = await ReadFileAsBase64Async(result.FullPath, cancellationToken);
                        fileList.Add(new MesFileEntry
                        {
                            FileCode = $"File{seq:D2}",
                            FileName = Path.GetFileNameWithoutExtension(result.FileName),
                            Remark = "",
                            FileUrl = result.RelativePath,
                            FileContent = base64,
                            FileType = "1"
                        });
                        seq++;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "[MES-FILE] Failed to read file content | FileCode={FileCode} | Path={Path}",
                        result.FileCode, result.FullPath);
                }
            }

            if (fileList.Count == 0)
            {
                _logger.LogWarning("[MES-FILE] No file entries built for MtrlCode={MtrlCode}, skipping", mtrlCode);
                continue;
            }

            var request = new MesFileUploadRequest
            {
                DocType = "CUST_FILE",
                UpdateType = "UPDATE",
                Data =
                [
                    new MesFileData
                    {
                        OrgId = "3701",
                        MtrlCode = mtrlCode,
                        FileList = fileList
                    }
                ]
            };

            var record = await PostToMesAsync(request, mtrlCode, cancellationToken);
            records.Add(record);
        }

        return records;
    }

    private async Task<MesUploadRecord> PostToMesAsync(MesFileUploadRequest request, string mtrlCode, CancellationToken cancellationToken)
    {
        var requestJson = JsonSerializer.Serialize(request, JsonOpts);
        var requestTime = DateTime.Now;

        _logger.LogInformation(
            "[MES-FILE REQUEST] Time: {Time} | Endpoint: {Endpoint} | MtrlCode: {MtrlCode} | FileCount: {Count}",
            requestTime.ToString("yyyy-MM-dd HH:mm:ss.fff"),
            _options.MesEndpoint,
            mtrlCode,
            request.Data[0].FileList.Count);

        try
        {
            var content = new StringContent(requestJson, Encoding.UTF8, "application/json");
            var response = await _http.PostAsync(_options.MesEndpoint, content, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            var responseTime = DateTime.Now;

            _logger.LogInformation(
                "[MES-FILE RESPONSE] Time: {Time} | Endpoint: {Endpoint} | MtrlCode: {MtrlCode} | HTTP Status: {Status} | Body: {Body}",
                responseTime.ToString("yyyy-MM-dd HH:mm:ss.fff"),
                _options.MesEndpoint,
                mtrlCode,
                (int)response.StatusCode,
                body);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError(
                    "[MES-FILE ERROR] MtrlCode: {MtrlCode} | HTTP {Status}: {Body}",
                    mtrlCode, (int)response.StatusCode, body);
            }

            return new MesUploadRecord(
                _options.MesEndpoint,
                mtrlCode,
                requestJson,
                (int)response.StatusCode,
                body,
                null,
                requestTime,
                responseTime
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[MES-FILE EXCEPTION] Time: {Time} | Endpoint: {Endpoint} | MtrlCode: {MtrlCode} | Error: {Error}",
                DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"),
                _options.MesEndpoint,
                mtrlCode,
                ex.Message);

            return new MesUploadRecord(
                _options.MesEndpoint,
                mtrlCode,
                requestJson,
                null,
                null,
                ex.Message,
                requestTime,
                DateTime.Now
            );
        }
    }

    private static async Task<string> ReadFileAsBase64Async(string filePath, CancellationToken cancellationToken)
    {
        var bytes = await File.ReadAllBytesAsync(filePath, cancellationToken);
        return Convert.ToBase64String(bytes);
    }

    private async Task<(List<MesFileEntry> Entries, int NextSeq)> ReadArchiveEntriesAsBase64Async(FileInfoResult result, int startSeq, CancellationToken cancellationToken)
    {
        var entries = new List<MesFileEntry>();
        var seq = startSeq;

        await Task.Run(() =>
        {
            using var archive = ArchiveFactory.OpenArchive(result.FullPath, new ReaderOptions());

            foreach (var entry in archive.Entries)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (entry.IsDirectory || entry.Size == 0)
                    continue;

                using var entryStream = entry.OpenEntryStream();
                using var ms = new MemoryStream();
                entryStream.CopyTo(ms);
                var base64 = Convert.ToBase64String(ms.ToArray());

                var entryName = entry.Key ?? "";
                var fileName = Path.GetFileName(entryName);

                entries.Add(new MesFileEntry
                {
                    FileCode = $"File{seq:D2}",
                    FileName = Path.GetFileNameWithoutExtension(fileName),
                    Remark = "",
                    FileUrl = result.RelativePath,
                    FileContent = base64,
                    FileType = "1"
                });
                seq++;
            }
        }, cancellationToken);

        return (entries, seq);
    }
}
