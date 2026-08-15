using System.Text.Json.Serialization;

namespace MdmSyncService.Models;

/// <summary>
/// Represents a single material master record to be synchronized with the MDM platform.
/// Each property maps directly to a JSON field expected by the MDM bulk-operation API.
/// Serialization uses snake_case naming to match the Java-originated contract.
/// </summary>
public sealed class MdmItem
{
    /// <summary>
    /// The new SAP material number assigned in Agile PLM (e.g. "X9841-0000428").
    /// Serves as the unique identifier for the material in MDM.
    /// </summary>
    [JsonPropertyName("new_sap_number")]
    public string NewSapNumber { get; init; } = string.Empty;

    /// <summary>
    /// Material group code derived from the first 5 characters of the Agile Class name.
    /// Corresponds to SAP field MATKL (e.g. "12345").
    /// </summary>
    [JsonPropertyName("new_material_group")]
    public string NewMaterialGroup { get; init; } = string.Empty;

    /// <summary>
    /// Legacy material number from the previous system. May be empty if no old number exists.
    /// Used by MDM to establish old-to-new number mappings.
    /// </summary>
    [JsonPropertyName("number")]
    public string Number { get; init; } = string.Empty;

    /// <summary>
    /// Material description in Chinese (e.g. "前轮轮毂总成-12寸").
    /// Sourced from Agile PLM attribute 1002 (Description).
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Base unit of measure (e.g. "PC", "KG", "M", "SET").
    /// Sourced from Agile PLM attribute 1271.
    /// </summary>
    [JsonPropertyName("basic_units")]
    public string BasicUnits { get; init; } = string.Empty;

    /// <summary>
    /// Product hierarchy code representing the classification level within Yadea's system.
    /// Sourced from Agile PLM attribute 1275 list API name (e.g. "YD01").
    /// </summary>
    [JsonPropertyName("new_product_code2")]
    public string NewProductCode2 { get; init; } = string.Empty;

    /// <summary>
    /// Industry sector identifier. Fixed value "M" (Manufacturing) for all synced materials.
    /// </summary>
    [JsonPropertyName("industry_sector")]
    public string IndustrySector { get; init; } = "M";

    /// <summary>
    /// Lifecycle state of the material.
    /// "在产" (active) for normal materials; "淘汰" (obsolete) for discontinued/EOL items.
    /// </summary>
    [JsonPropertyName("state")]
    public string State { get; init; } = string.Empty;

    /// <summary>
    /// Applicable factory codes, semicolon-separated (e.g. "1000;2000;3000").
    /// Sourced from Agile PLM attribute 2090 redline value. Sent as a single string (not split).
    /// </summary>
    [JsonPropertyName("factory")]
    public string Factory { get; init; } = string.Empty;

    /// <summary>
    /// Supplementary product description. Fixed value "配件" (accessory/part) for all synced materials.
    /// </summary>
    [JsonPropertyName("product_description2")]
    public string ProductDescription2 { get; init; } = "配件";

    /// <summary>
    /// Material type code from Agile PLM attribute 1274 list API name.
    /// Common values: "ZROH" (raw material), "ZHAL" (semi-finished), "ZFERT" (finished goods).
    /// </summary>
    [JsonPropertyName("material_type")]
    public string MaterialType { get; init; } = string.Empty;

    /// <summary>
    /// Parts ownership classification.
    /// "雅迪零部件" for Yadea-managed parts (ATT_PAGE_TWO_LIST01 = "Y");
    /// "凌博零部件" for Lingbo/Huayu-managed parts.
    /// </summary>
    [JsonPropertyName("parts_mat_type")]
    public string PartsMatType { get; init; } = string.Empty;
}
