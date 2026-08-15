using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using PlmMesSync.Models.Dto;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace PlmMesSync.Services;

public class MesUploadService
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    private readonly HttpClient _http;
    private readonly ILogger<MesUploadService> _logger;
    private readonly string _endpoint;

    public MesUploadService(HttpClient http, IConfiguration configuration, ILogger<MesUploadService> logger)
    {
        _http = http;
        _logger = logger;
        _endpoint = configuration["SyncSettings:MesEndpoint"]
            ?? AppConstants.DefaultMesEndpoint;
    }

    public async Task<MesUploadResult> UploadBomToMes(BomSyncOutput output)
    {
        var requestTime = DateTime.Now;
        var requestJson = "";

        try
        {
            var latestVersion = output.Versions
                .Where(v => v.IsLatest)
                .OrderByDescending(v => v.ReleaseDate ?? DateTime.MinValue)
                .FirstOrDefault();

            if (latestVersion is null || latestVersion.Lines.Count == 0)
            {
                _logger.LogWarning("[MES] No active BOM lines in latest version, skipping upload for {Item}",
                    output.ParentItemNumber);
                return new MesUploadResult
                {
                    Success = false, Error = "No active BOM lines in latest version",
                    Endpoint = _endpoint, RequestJson = "", RequestTime = requestTime, ResponseTime = DateTime.Now
                };
            }

            var request = BuildRequest(output.ParentItemNumber, latestVersion);
            requestJson = JsonSerializer.Serialize(request, JsonOpts);

            _logger.LogInformation(
                "[MES REQUEST] Time: {Time} | Endpoint: {Endpoint} | Item: {Item} | Rev: {Rev} | Lines: {Count}\n{Json}",
                requestTime.ToString("yyyy-MM-dd HH:mm:ss.fff"),
                _endpoint,
                output.ParentItemNumber,
                latestVersion.RevNumber,
                request.Data[0].BsBomMtrl.Count,
                requestJson);

            var (success, statusCode, body, responseTime) = await SendWithRetryAsync(requestJson, output.ParentItemNumber);

            _logger.LogInformation(
                "[MES RESPONSE] Time: {Time} | Endpoint: {Endpoint} | Item: {Item} | HTTP Status: {Status} | Body: {Body}",
                responseTime.ToString("yyyy-MM-dd HH:mm:ss.fff"),
                _endpoint,
                output.ParentItemNumber,
                statusCode,
                body);

            return new MesUploadResult
            {
                Success = success,
                Error = success ? null : $"HTTP {statusCode}: {body}",
                Endpoint = _endpoint,
                RequestJson = requestJson,
                HttpStatus = statusCode,
                ResponseBody = body,
                RequestTime = requestTime,
                ResponseTime = responseTime
            };
        }
        catch (Exception ex)
        {
            var responseTime = DateTime.Now;
            _logger.LogError(ex,
                "[MES EXCEPTION] Time: {Time} | Endpoint: {Endpoint} | Item: {Item} | Error: {Error}",
                responseTime.ToString("yyyy-MM-dd HH:mm:ss.fff"),
                _endpoint,
                output.ParentItemNumber,
                ex.Message);

            return new MesUploadResult
            {
                Success = false, Error = ex.Message,
                Endpoint = _endpoint, RequestJson = requestJson,
                RequestTime = requestTime, ResponseTime = responseTime
            };
        }
    }

    private async Task<(bool Success, int StatusCode, string Body, DateTime ResponseTime)> SendWithRetryAsync(
        string requestJson, string itemNumber)
    {
        var maxRetries = AppConstants.DefaultRetryCount;

        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                _logger.LogInformation(
                    "[MES-RETRY] Sending to MES (attempt {Attempt}/{Max}): {Item}",
                    attempt, maxRetries, itemNumber);

                var content = new StringContent(requestJson, Encoding.UTF8, "application/json");
                var response = await _http.PostAsync(_endpoint, content);
                var body = await response.Content.ReadAsStringAsync();
                var responseTime = DateTime.Now;

                if (response.IsSuccessStatusCode)
                    return (true, (int)response.StatusCode, body, responseTime);

                if (attempt < maxRetries && (int)response.StatusCode >= 500)
                {
                    var delay = TimeSpan.FromSeconds(attempt * 3);
                    _logger.LogWarning(
                        "[MES-RETRY] Server error {Status} on attempt {Attempt}/{Max}, retrying in {Delay}s...",
                        (int)response.StatusCode, attempt, maxRetries, delay.TotalSeconds);
                    await Task.Delay(delay);
                    continue;
                }

                return (false, (int)response.StatusCode, body, responseTime);
            }
            catch (HttpRequestException ex) when (attempt < maxRetries)
            {
                var delay = TimeSpan.FromSeconds(attempt * 3);
                _logger.LogWarning(
                    "[MES-RETRY] HTTP error on attempt {Attempt}/{Max}: {Error}, retrying in {Delay}s...",
                    attempt, maxRetries, ex.Message, delay.TotalSeconds);
                await Task.Delay(delay);
            }
            catch (TaskCanceledException ex) when (attempt < maxRetries)
            {
                var delay = TimeSpan.FromSeconds(attempt * 3);
                _logger.LogWarning(
                    "[MES-RETRY] Timeout on attempt {Attempt}/{Max}, retrying in {Delay}s...",
                    attempt, maxRetries, delay.TotalSeconds);
                await Task.Delay(delay);
            }
        }

        return (false, 0, "All retry attempts exhausted", DateTime.Now);
    }

    private static MesUploadRequest BuildRequest(string itemNumber, RevVersionOutput version)
    {
        var lines = version.Lines;

        var mainCodeByGroup = new Dictionary<string, string>();
        foreach (var line in lines)
        {
            if (!string.IsNullOrEmpty(line.SubstituteGroup) && line.SubstitutePriority == AppConstants.SubstitutePriorityPrimary)
            {
                mainCodeByGroup.TryAdd(line.SubstituteGroup, line.ItemNumber ?? "");
            }
        }

        var mtrlList = new List<MesBomMtrl>();
        foreach (var line in lines)
        {
            string isMain;
            string mainCode;

            if (string.IsNullOrEmpty(line.SubstituteGroup))
            {
                isMain = AppConstants.IsMainYes;
                mainCode = line.ItemNumber ?? "";
            }
            else if (line.SubstitutePriority == AppConstants.SubstitutePriorityPrimary)
            {
                isMain = AppConstants.IsMainYes;
                mainCode = line.ItemNumber ?? "";
            }
            else
            {
                isMain = AppConstants.IsMainNo;
                mainCode = mainCodeByGroup.GetValueOrDefault(line.SubstituteGroup, line.ItemNumber ?? "");
            }

            decimal dosage = 0;
            if (!string.IsNullOrEmpty(line.Quantity))
                decimal.TryParse(line.Quantity, out dosage);

            mtrlList.Add(new MesBomMtrl
            {
                MtrlCode = line.ItemNumber ?? "",
                IsMain = isMain,
                MainCode = mainCode,
                Dosage = dosage,
                PointStr = string.IsNullOrEmpty(line.RefDesig) ? "" : line.RefDesig,
                MbomVer = string.IsNullOrEmpty(line.ComponentRev) ? "" : line.ComponentRev,
                Remark = string.IsNullOrEmpty(line.Description) ? "" : line.Description
            });
        }

        var prodDesc = version.Lines.FirstOrDefault()?.Description ?? "";
        return new MesUploadRequest
        {
            DocType = AppConstants.DocTypeBom,
            UpdateType = AppConstants.UpdateType,
            Data =
            [
                new()
                {
                    OrgCode = AppConstants.OrgCode,
                    ProdCode = itemNumber,
                    BomVer = version.RevNumber,
                    IsDef = AppConstants.IsMainYes,
                    IsValid = AppConstants.IsMainYes,
                    Remark = prodDesc,
                    BsBomMtrl = mtrlList
                }
            ]
        };
    }
}

public class MesUploadResult
{
    public bool Success { get; set; }
    public string? Error { get; set; }
    public string Endpoint { get; set; } = string.Empty;
    public string RequestJson { get; set; } = string.Empty;
    public int? HttpStatus { get; set; }
    public string? ResponseBody { get; set; }
    public DateTime RequestTime { get; set; }
    public DateTime ResponseTime { get; set; }
}
