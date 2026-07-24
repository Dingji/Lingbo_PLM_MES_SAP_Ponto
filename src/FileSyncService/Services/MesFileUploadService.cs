using FileSyncService.Models.Dto;

using Microsoft.Extensions.Options;

using System.Text;
using System.Text.Json;

namespace FileSyncService.Services;

public sealed class MesFileUploadOptions
{
    public const string SectionName = "MesUploadSettings";

    public string MesEndpoint { get; set; } = AppConstants.DefaultMesEndpoint;

    public int TimeoutSeconds { get; set; } = AppConstants.DefaultTimeoutSeconds;
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

        var grouped = successResults.GroupBy(r => r.InventoryCode);

        foreach (var group in grouped)
        {
            var mtrlCode = group.Key;
            var fileList = new List<MesFileEntry>();
            var seq = 1;

            foreach (var result in group)
            {
                try
                {
                    var candidates = await GetCandidateFilesAsync(result, cancellationToken);

                    foreach (var (name, bytes) in candidates)
                    {
                        var fileType = FileTypeClassifier.DetermineFileType(name, result.FileCode);

                        if (!FileTypeClassifier.ShouldTransmit(name, fileType))
                        {
                            _logger.LogInformation(
                                "[MES-FILE] Skip non-PDF | FileCode={FileCode} | File={Name} | FileType={FileType}",
                                result.FileCode, name, fileType);
                            continue;
                        }

                        if (bytes.Length > AppConstants.MaxFileSizeBytes)
                        {
                            _logger.LogWarning(
                                "[MES-FILE] Skip oversized file | FileCode={FileCode} | File={Name} | Size={Size} | Max={Max}",
                                result.FileCode, name, bytes.Length, AppConstants.MaxFileSizeBytes);
                            continue;
                        }

                        fileList.Add(new MesFileEntry
                        {
                            FileCode = $"File{seq:D2}",
                            FileName = Path.GetFileNameWithoutExtension(name),
                            Remark = "",
                            FileUrl = result.RelativePath,
                            FileContent = Convert.ToBase64String(bytes),
                            FileType = fileType
                        });
                        seq++;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "[MES-FILE] Failed to read file content | FileCode={FileCode} | FileName={FileName}",
                        result.FileCode, result.FileName);
                }
            }

            if (fileList.Count == 0)
            {
                _logger.LogWarning("[MES-FILE] No file entries built for MtrlCode={MtrlCode}, skipping", mtrlCode);
                continue;
            }

            var request = new MesFileUploadRequest
            {
                DocType = AppConstants.DocTypeFile,
                UpdateType = AppConstants.UpdateType,
                Data =
                [
                    new MesFileData
                    {
                        OrgId = AppConstants.OrgCode,
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
                "[MES-FILE RESPONSE] Time: {Time} | Endpoint: {Endpoint} | MtrlCode: {MtrlCode} | HTTP Status: {Status}",
                responseTime.ToString("yyyy-MM-dd HH:mm:ss.fff"),
                _options.MesEndpoint,
                mtrlCode,
                (int)response.StatusCode);

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
                "[MES-FILE EXCEPTION] Time: {Time} | Endpoint: {Endpoint} | MtrlCode: {MtrlCode}",
                DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"),
                _options.MesEndpoint,
                mtrlCode);

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

    private static async Task<List<(string Name, byte[] Bytes)>> GetCandidateFilesAsync(
        FileInfoResult result, CancellationToken cancellationToken)
    {
        if (result.IsArchiveFile)
        {
            var leaves = await Task.Run(
                () => ArchiveExtractor.ExtractLeaves(result.FullPath, cancellationToken), cancellationToken);

            return leaves
                .Where(l => l.Bytes.Length > 0)
                .Select(l => (l.Name, l.Bytes))
                .ToList();
        }

        var bytes = await File.ReadAllBytesAsync(result.FullPath, cancellationToken);
        return [(result.FileName, bytes)];
    }
}
