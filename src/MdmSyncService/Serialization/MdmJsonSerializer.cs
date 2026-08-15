using System.Text.Json;

namespace MdmSyncService.Serialization;

/// <summary>
/// Centralised JSON serialization options shared across the service.
/// Ensures consistent snake_case naming and null-value handling between
/// MdmSyncService (HTTP send) and SqliteCacheStore (persistence).
/// </summary>
public static class MdmJsonSerializer
{
    /// <summary>
    /// Pre-configured options: snake_case property naming, skip nulls, no indentation.
    /// </summary>
    public static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false
    };
}
