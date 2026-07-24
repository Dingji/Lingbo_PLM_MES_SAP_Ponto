using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace PlmMesSync.Models.Dto;

public class MesUploadRequest
{
    [JsonPropertyName("docType")]
    public string DocType { get; set; } = "BS_BOM";

    [JsonPropertyName("updateType")]
    public string UpdateType { get; set; } = "UPDATE";

    [JsonPropertyName("data")]
    public List<MesBomData> Data { get; set; } = new();
}

public class MesBomData
{
    [JsonPropertyName("org_code")]
    public string OrgCode { get; set; } = "3701";

    [JsonPropertyName("prod_code")]
    public string ProdCode { get; set; } = string.Empty;

    [JsonPropertyName("bom_ver")]
    public string BomVer { get; set; } = string.Empty;

    [JsonPropertyName("is_def")]
    public string IsDef { get; set; } = "y";

    [JsonPropertyName("is_valid")]
    public string IsValid { get; set; } = "y";

    [JsonPropertyName("remark")]
    public string? Remark { get; set; }

    [JsonPropertyName("bs_bom_mtrl")]
    public List<MesBomMtrl> BsBomMtrl { get; set; } = new();
}

public class MesBomMtrl
{
    [JsonPropertyName("mtrl_code")]
    public string MtrlCode { get; set; } = string.Empty;

    [JsonPropertyName("is_main")]
    public string IsMain { get; set; } = "y";

    [JsonPropertyName("main_code")]
    public string MainCode { get; set; } = string.Empty;

    [JsonPropertyName("dosage")]
    public decimal Dosage { get; set; }

    [JsonPropertyName("point_str")]
    public string? PointStr { get; set; }

    [JsonPropertyName("mbom_ver")]
    public string? MbomVer { get; set; }

    [JsonPropertyName("remark")]
    public string? Remark { get; set; }
}
