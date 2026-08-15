using System.Threading.Channels;
using Microsoft.Extensions.Http.Resilience;
using MdmSyncService.Configuration;
using MdmSyncService.Models;
using MdmSyncService.Persistence;
using MdmSyncService.Services;
using Polly;
using Polly.Timeout;
using Serilog;
using MdmSyncSvc = MdmSyncService.Services.MdmSyncService;

// ============================================================================
// MdmSyncService — Windows Service for synchronizing material master data
// with Yadea's MDM platform via HTTP POST.
//
// Architecture:
//   Producer (external) → Channel<MdmItem[]> → MdmWorker → MdmSyncService → MDM API
//                                                    │ (on failure)
//                                                    ▼
//                                          SQLite Cache (versioned)
//                                                    │
//                                                    ▼
//                                          RetryWorker (periodic scan)
//
// Resilience pipeline (attached to the named HttpClient):
//   Timeout (30s) → Retry (3x exponential) → Circuit Breaker (5 failures, 30s break)
//
// Persistence: SQLite WAL-mode cache with version deduplication and dead-letter.
// Logging: Serilog with rolling file + console sinks.
// Hosting: Runs as a Windows Service or as a console app for development.
// ============================================================================

// --- Configure Serilog before anything else ---
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Warning)
    .MinimumLevel.Override("System.Net.Http", Serilog.Events.LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .WriteTo.Console(
        outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff} {Level:u3}] {Message:lj}{NewLine}{Exception}")
    .WriteTo.File(
        path: "logs/mdmsync-.log",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 30,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {SourceContext}{NewLine}  {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

try
{
    Log.Information("MdmSyncService starting up...");

    // --- Build the host ---
    HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

    // Enable Windows Service hosting (no-op when running as console)
    builder.Services.AddWindowsService(options =>
    {
        options.ServiceName = "MdmSyncService";
    });

    // Integrate Serilog with the generic host
    builder.Services.AddSerilog();

    // --- Bind configuration ---
    builder.Services.Configure<MdmOptions>(
        builder.Configuration.GetSection(MdmOptions.SectionName));

    // --- Register the bounded channel for thread-safe producer/consumer communication ---
    // Capacity of 100 batches provides back-pressure: if the consumer falls behind,
    // producers will block (or fail fast) rather than consuming unbounded memory.
    Channel<MdmItem[]> channel = Channel.CreateBounded<MdmItem[]>(new BoundedChannelOptions(capacity: 100)
    {
        FullMode = BoundedChannelFullMode.Wait,       // Producers wait when full
        SingleReader = true,                          // Only MdmWorker reads
        SingleWriter = false                          // Multiple producers may write
    });

    // Register channel reader/writer as singletons for DI injection
    builder.Services.AddSingleton(channel.Reader);
    builder.Services.AddSingleton(channel.Writer);

    // --- Register the named HttpClient with resilience pipeline ---
    builder.Services.AddHttpClient(MdmSyncSvc.HttpClientName, client =>
    {
        // Base address is set per-request in MdmSyncService, but we configure defaults here.
        client.Timeout = Timeout.InfiniteTimeSpan; // Timeout is handled by the resilience pipeline
    })
    .AddResilienceHandler("mdm-pipeline", pipelineBuilder =>
    {
        // Layer 1: Per-attempt timeout.
        // If a single HTTP attempt takes longer than 30 seconds, cancel it.
        pipelineBuilder.AddTimeout(TimeSpan.FromSeconds(30));

        // Layer 2: Retry with exponential backoff.
        // Retries transient failures (5xx, 408, HttpRequestException, TimeoutRejectedException).
        // Delays: ~1s, ~2s, ~4s (with jitter to avoid thundering herd).
        pipelineBuilder.AddRetry(new HttpRetryStrategyOptions
        {
            MaxRetryAttempts = 3,
            Delay = TimeSpan.FromSeconds(1),
            BackoffType = DelayBackoffType.Exponential,
            UseJitter = true,
            ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                .Handle<HttpRequestException>()
                .Handle<TimeoutRejectedException>()
                .HandleResult(response => (int)response.StatusCode >= 500)
                .HandleResult(response => response.StatusCode == System.Net.HttpStatusCode.RequestTimeout)
        });

        // Layer 3: Circuit breaker.
        // After 5 consecutive failures within a 30-second sampling window,
        // the circuit opens for 30 seconds — all calls fail fast without hitting the server.
        // This protects the MDM platform from being hammered during outages.
        pipelineBuilder.AddCircuitBreaker(new HttpCircuitBreakerStrategyOptions
        {
            FailureRatio = 0.5,
            MinimumThroughput = 5,
            SamplingDuration = TimeSpan.FromSeconds(30),
            BreakDuration = TimeSpan.FromSeconds(30),
            ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                .Handle<HttpRequestException>()
                .Handle<TimeoutRejectedException>()
                .HandleResult(response => (int)response.StatusCode >= 500)
        });
    });

    // --- Register application services ---
    builder.Services.AddSingleton<ICacheStore, SqliteCacheStore>();
    builder.Services.AddSingleton<IMdmSyncService, MdmSyncService.Services.MdmSyncService>();
    builder.Services.AddHostedService<MdmWorker>();
    builder.Services.AddHostedService<RetryWorker>();

    // --- Build and run ---
    IHost host = builder.Build();

    // Graceful shutdown: complete the channel writer so MdmWorker drains, then checkpoint SQLite.
    IHostApplicationLifetime lifetime = host.Services.GetRequiredService<IHostApplicationLifetime>();
    lifetime.ApplicationStopping.Register(() =>
    {
        channel.Writer.TryComplete();
        var cacheStore = host.Services.GetRequiredService<ICacheStore>();
        cacheStore.CheckpointAsync(CancellationToken.None).GetAwaiter().GetResult();
    });

    await host.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "MdmSyncService terminated unexpectedly during startup.");
}
finally
{
    // Flush and close Serilog sinks to ensure all log entries are persisted.
    await Log.CloseAndFlushAsync();
}
