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
            ?? "http://10.170.9.17:8888/ims-integrate/api/updateImsData";
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

            var content = new StringContent(requestJson, Encoding.UTF8, "application/json");
            var response = await _http.PostAsync(_endpoint, content);
            var body = await response.Content.ReadAsStringAsync();
            var responseTime = DateTime.Now;

            _logger.LogInformation(
                "[MES RESPONSE] Time: {Time} | Endpoint: {Endpoint} | Item: {Item} | HTTP Status: {Status} | Body: {Body}",
                responseTime.ToString("yyyy-MM-dd HH:mm:ss.fff"),
                _endpoint,
                output.ParentItemNumber,
                (int)response.StatusCode,
                body);

            return new MesUploadResult
            {
                Success = response.IsSuccessStatusCode,
                Error = response.IsSuccessStatusCode ? null : $"HTTP {(int)response.StatusCode}: {body}",
                Endpoint = _endpoint,
                RequestJson = requestJson,
                HttpStatus = (int)response.StatusCode,
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

    private static MesUploadRequest BuildRequest(string itemNumber, RevVersionOutput version)
    {
        var lines = version.Lines;

        var mainCodeByGroup = new Dictionary<string, string>();
        foreach (var line in lines)
        {
            if (!string.IsNullOrEmpty(line.SubstituteGroup) && line.SubstitutePriority == "1")
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
                isMain = "y";
                mainCode = line.ItemNumber ?? "";
            }
            else if (line.SubstitutePriority == "1")
            {
                isMain = "y";
                mainCode = line.ItemNumber ?? "";
            }
            else
            {
                isMain = "n";
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
                PointStr = string.IsNullOrEmpty(line.RefDesig) ? null : line.RefDesig,
                MbomVer = line.ComponentRev,
                Remark = line.Description ?? ""
            });
        }

        var prodDesc = version.Lines.FirstOrDefault()?.Description ?? "";
        return new MesUploadRequest
        {
            DocType = "BS_BOM",
            UpdateType = "UPDATE",
            Data = new List<MesBomData>
            {
                new()
                {
                    OrgCode = "3701",
                    ProdCode = itemNumber,
                    BomVer = version.RevNumber,
                    IsDef = "y",
                    IsValid = "y",
                    Remark = prodDesc,
                    BsBomMtrl = mtrlList
                }
            }
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
