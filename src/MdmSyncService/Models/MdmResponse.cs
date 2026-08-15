using System.Text.Json.Serialization;

namespace MdmSyncService.Models;

/// <summary>
/// Represents the JSON response returned by the MDM bulk-operation API.
/// A successful call returns code 200; any other code indicates failure.
/// </summary>
public sealed class MdmResponse
{
    /// <summary>
    /// HTTP-level business status code from the MDM platform.
    /// 200 = all materials processed successfully; non-200 = failure.
    /// </summary>
    [JsonPropertyName("code")]
    public int? Code { get; init; }

    /// <summary>
    /// Human-readable message describing the result or error reason.
    /// </summary>
    [JsonPropertyName("message")]
    public string? Message { get; init; }

    /// <summary>
    /// Returns true if the MDM platform reported successful processing.
    /// </summary>
    [JsonIgnore]
    public bool IsSuccess => Code == 200;
}
