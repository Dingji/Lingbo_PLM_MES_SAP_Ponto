using System.Text.Json.Serialization;

namespace FileSyncService.Models.Dto;

public sealed record MesUploadRecord(
    string Endpoint,
    string MtrlCode,
    string RequestJson,
    int? HttpStatus,
    string? ResponseBody,
    string? Error,
    DateTime RequestTime,
    DateTime ResponseTime
);

public sealed class MesFileUploadRequest
{
    [JsonPropertyName("docType")]
    public string DocType { get; set; } = AppConstants.DocTypeFile;

    [JsonPropertyName("updateType")]
    public string UpdateType { get; set; } = AppConstants.UpdateType;

    [JsonPropertyName("data")]
    public List<MesFileData> Data { get; set; } = [];
}

public sealed class MesFileData
{
    [JsonPropertyName("ORG_ID")]
    public string OrgId { get; set; } = AppConstants.OrgCode;

    [JsonPropertyName("MTRL_CODE")]
    public string MtrlCode { get; set; } = string.Empty;

    [JsonPropertyName("file_list")]
    public List<MesFileEntry> FileList { get; set; } = [];
}

public sealed class MesFileEntry
{
    [JsonPropertyName("file_code")]
    public string FileCode { get; set; } = string.Empty;

    [JsonPropertyName("file_name")]
    public string FileName { get; set; } = string.Empty;

    [JsonPropertyName("remark")]
    public string Remark { get; set; } = string.Empty;

    [JsonPropertyName("file_url")]
    public string FileUrl { get; set; } = string.Empty;

    [JsonPropertyName("file_content")]
    public string FileContent { get; set; } = string.Empty;

    [JsonPropertyName("file_type")]
    public string FileType { get; set; } = "1";
}
