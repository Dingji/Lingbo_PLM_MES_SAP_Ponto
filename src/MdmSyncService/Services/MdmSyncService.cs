using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using MdmSyncService.Configuration;
using MdmSyncService.Models;
using MdmSyncService.Serialization;

namespace MdmSyncService.Services;

/// <summary>
/// Core implementation of the MDM synchronization service.
/// Responsible for serializing material data to JSON, constructing the authenticated
/// HTTP POST request, executing it via a resilient named HttpClient, and validating
/// the response from the MDM platform.
///
/// Thread safety: This class is registered as a singleton. It holds no mutable state;
/// all dependencies (HttpClientFactory, Options, Logger) are themselves thread-safe.
/// </summary>
public sealed class MdmSyncService : IMdmSyncService
{
    // Named client identifier used with IHttpClientFactory for connection pooling
    // and resilience pipeline attachment.
    public const string HttpClientName = "MdmClient";

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly MdmOptions _options;
    private readonly ILogger<MdmSyncService> _logger;

    // Serialization options are shared project-wide via MdmJsonSerializer.

    public MdmSyncService(
        IHttpClientFactory httpClientFactory,
        IOptions<MdmOptions> options,
        ILogger<MdmSyncService> logger)
    {
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<MdmResponse> SendAsync(IReadOnlyList<MdmItem> items, CancellationToken cancellationToken = default)
    {
        // --- Input validation ---
        ArgumentNullException.ThrowIfNull(items);
        if (items.Count == 0)
        {
            throw new ArgumentException("Cannot send an empty batch to MDM.", nameof(items));
        }

        // --- Step 1: Serialize the item list to a JSON array string (UTF-8) ---
        // System.Text.Json handles snake_case naming via JsonPropertyName attributes on MdmItem.
        // Null properties are omitted to keep the payload compact.
        string jsonPayload = JsonSerializer.Serialize(items, MdmJsonSerializer.Options);

        _logger.LogDebug(
            "Serialized {ItemCount} material(s) to JSON payload ({PayloadLength} bytes)",
            items.Count, Encoding.UTF8.GetByteCount(jsonPayload));

        // --- Step 2: Construct the HTTP POST request with required headers ---
        string requestUrl = _options.GetFullUrl();
        using var request = new HttpRequestMessage(HttpMethod.Post, requestUrl);

        // Accept header: we expect a JSON response
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        // MDM authentication headers: client identity and environment-specific secret
        request.Headers.TryAddWithoutValidation("x-mdm-client-code", _options.ClientCode);
        request.Headers.TryAddWithoutValidation("x-mdm-client-secret", _options.GetClientSecret());

        // Request body: JSON array encoded as UTF-8 (Content-Type matches Java MdmUtil format)
        request.Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json;charset=UTF-8");

        _logger.LogInformation(
            "Sending POST request to MDM: {Url} | Environment: {Env} | Items: {ItemCount}",
            requestUrl, _options.Environment, items.Count);

        // --- Step 3: Execute the request via the named HttpClient ---
        // The named client "MdmClient" has a resilience pipeline attached (retry, circuit breaker, timeout)
        // configured in Program.cs via Microsoft.Extensions.Http.Resilience.
        HttpClient client = _httpClientFactory.CreateClient(HttpClientName);
        HttpResponseMessage response;

        try
        {
            response = await client.SendAsync(request, cancellationToken).ConfigureAwait(false);
        }
        catch (HttpRequestException ex)
        {
            // Transport-level failure (DNS, connection refused, TLS error)
            _logger.LogError(ex, "HTTP transport error while calling MDM at {Url}", requestUrl);
            throw new MdmSyncException(
                $"Failed to connect to MDM platform at {requestUrl}: {ex.Message}",
                innerException: ex);
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            // Timeout (not a user-initiated cancellation)
            _logger.LogError(ex, "Request to MDM timed out after configured limit");
            throw new MdmSyncException(
                $"Request to MDM platform timed out. URL: {requestUrl}",
                innerException: ex);
        }

        // --- Step 4: Read and parse the response ---
        using (response)
        {
            string responseBody = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

            _logger.LogDebug("MDM raw response (HTTP {StatusCode}): {Body}",
                (int)response.StatusCode, responseBody);

            // Handle non-2xx HTTP status codes
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError(
                    "MDM returned HTTP {StatusCode}. Body: {Body}",
                    (int)response.StatusCode, responseBody);
                throw new MdmSyncException(
                    $"MDM platform returned HTTP {(int)response.StatusCode}. Response: {responseBody}",
                    mdmCode: (int)response.StatusCode);
            }

            // Deserialize the JSON response body
            MdmResponse? mdmResponse;
            try
            {
                mdmResponse = JsonSerializer.Deserialize<MdmResponse>(responseBody, MdmJsonSerializer.Options);
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to parse MDM response as JSON: {Body}", responseBody);
                throw new MdmSyncException(
                    $"MDM returned malformed JSON: {responseBody}",
                    innerException: ex);
            }

            if (mdmResponse is null)
            {
                throw new MdmSyncException("MDM returned a null response body.");
            }

            // --- Step 5: Validate the business-level status code ---
            if (mdmResponse.Code is null)
            {
                _logger.LogError("MDM response missing 'code' field. Body: {Body}", responseBody);
                throw new MdmSyncException("MDM response did not contain a status code.");
            }

            if (!mdmResponse.IsSuccess)
            {
                _logger.LogError(
                    "MDM integration failed. Code: {Code}, Message: {Message}",
                    mdmResponse.Code, mdmResponse.Message);
                throw new MdmSyncException(
                    $"MDM integration failed (code={mdmResponse.Code}): {mdmResponse.Message}",
                    mdmCode: mdmResponse.Code);
            }

            _logger.LogInformation(
                "MDM sync succeeded for {ItemCount} item(s). Message: {Message}",
                items.Count, mdmResponse.Message);

            return mdmResponse;
        }
    }
}
