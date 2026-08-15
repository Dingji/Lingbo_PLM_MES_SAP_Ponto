namespace MdmSyncService.Configuration;

/// <summary>
/// Strongly-typed configuration options for the MDM integration endpoint.
/// Bound from the "Mdm" section of appsettings.json.
/// </summary>
public sealed class MdmOptions
{
    /// <summary>
    /// Configuration section name used for binding.
    /// </summary>
    public const string SectionName = "Mdm";

    /// <summary>
    /// Deployment environment identifier. "pro" selects the production secret;
    /// any other value selects the test secret.
    /// </summary>
    public string Environment { get; init; } = "test";

    /// <summary>
    /// Base URL of the MDM platform (e.g. "https://mdm.yadea.com.cn").
    /// </summary>
    public string BaseUrl { get; init; } = string.Empty;

    /// <summary>
    /// API endpoint path appended to BaseUrl
    /// (e.g. "/yadeaMdm/mdm/yadeaMat/matDataBulkOperation").
    /// </summary>
    public string Endpoint { get; init; } = string.Empty;

    /// <summary>
    /// Client identification code sent in the x-mdm-client-code header.
    /// Identifies this integration to the MDM platform.
    /// </summary>
    public string ClientCode { get; init; } = string.Empty;

    /// <summary>
    /// Client secret for the production environment.
    /// Sent in the x-mdm-client-secret header when Environment == "pro".
    /// </summary>
    public string ClientSecretPro { get; init; } = string.Empty;

    /// <summary>
    /// Client secret for the test/development environment.
    /// Sent in the x-mdm-client-secret header when Environment != "pro".
    /// </summary>
    public string ClientSecretTest { get; init; } = string.Empty;

    /// <summary>
    /// Persistent cache and retry configuration.
    /// </summary>
    public CacheOptions Cache { get; init; } = new();

    /// <summary>
    /// Constructs the full request URL by combining BaseUrl and Endpoint.
    /// </summary>
    public string GetFullUrl() => $"{BaseUrl.TrimEnd('/')}{Endpoint}";

    /// <summary>
    /// Returns the appropriate client secret based on the configured environment.
    /// </summary>
    public string GetClientSecret() =>
        string.Equals(Environment, "pro", StringComparison.OrdinalIgnoreCase)
            ? ClientSecretPro
            : ClientSecretTest;

    /// <summary>
    /// Configuration for the local SQLite cache and automatic retry behaviour.
    /// </summary>
    public sealed class CacheOptions
    {
        /// <summary>
        /// Relative or absolute path to the SQLite database file.
        /// </summary>
        public string DatabasePath { get; init; } = "data/mdm_cache.db";

        /// <summary>
        /// How often (seconds) the RetryWorker scans for due items.
        /// </summary>
        public int RetryIntervalSeconds { get; init; } = 60;

        /// <summary>
        /// Maximum number of items sent in a single HTTP batch during retry.
        /// </summary>
        public int MaxBatchSize { get; init; } = 50;

        /// <summary>
        /// Maximum number of items sent in a single HTTP batch during the initial send
        /// (before any cache/retry path). Oversized batches are split transparently.
        /// Default 0 means no limit (behaviour prior to Bug 4 fix).
        /// </summary>
        public int MaxInitialBatchSize { get; init; } = 200;

        /// <summary>
        /// After this many failed attempts, an item is moved to the dead-letter table.
        /// </summary>
        public int MaxAttempts { get; init; } = 100;

        /// <summary>
        /// Base delay (seconds) for exponential backoff between retries.
        /// Actual delay = min(2^attempt * BaseBackoffSeconds, MaxBackoffHours * 3600).
        /// </summary>
        public int BaseBackoffSeconds { get; init; } = 60;

        /// <summary>
        /// Upper bound (hours) for the exponential backoff delay.
        /// </summary>
        public int MaxBackoffHours { get; init; } = 24;
    }
}
