using FileSyncService;
using FileSyncService.Endpoints;
using FileSyncService.Services;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using Serilog;

using System;
using System.IO;

Directory.SetCurrentDirectory(AppContext.BaseDirectory);

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(new WebApplicationOptions
    {
        Args = args,
        ContentRootPath = AppContext.BaseDirectory,
        WebRootPath = Path.Combine(AppContext.BaseDirectory, "wwwroot")
    });

    builder.Host.UseWindowsService(options => options.ServiceName = "FileSyncService");

    builder.WebHost.ConfigureKestrel(k => k.ListenAnyIP(31457));

    builder.Host.UseSerilog((context, configuration) =>
        configuration.ReadFrom.Configuration(context.Configuration));

    builder.Services.Configure<FileSyncOptions>(
        builder.Configuration.GetSection(FileSyncOptions.SectionName));
    builder.Services.Configure<MesFileUploadOptions>(
        builder.Configuration.GetSection(MesFileUploadOptions.SectionName));

    builder.Services.AddSingleton<RequestStore>();
    builder.Services.AddSingleton<FileSyncProcessor>();
    builder.Services.AddSingleton<FileSyncFileLogger>();
    builder.Services.AddSingleton<LogReaderService>();
    builder.Services.AddHttpClient<MesFileUploadService>((sp, client) =>
    {
        var opts = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<MesFileUploadOptions>>().Value;
        client.Timeout = TimeSpan.FromSeconds(opts.TimeoutSeconds);
    });

    var downloadTimeout = builder.Configuration.GetValue("FileSyncSettings:DownloadTimeoutSeconds", AppConstants.DefaultDownloadTimeoutSeconds);
    builder.Services.AddHttpClient("FileSyncDownload", client =>
    {
        client.Timeout = TimeSpan.FromSeconds(downloadTimeout);
        client.DefaultRequestHeaders.Add("User-Agent", "FileSyncService/1.0");
    })
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
    {
        AutomaticDecompression = System.Net.DecompressionMethods.All
    });

    var app = builder.Build();

    var rootDir = builder.Configuration.GetValue<string>("FileSyncSettings:RootDirectory") ?? AppConstants.DefaultRootDir;
    if (!Directory.Exists(rootDir))
    {
        Directory.CreateDirectory(rootDir);
    }

    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    logger.LogInformation("========================================================");
    logger.LogInformation("[STARTUP] FileSyncService starting");
    logger.LogInformation("[STARTUP] Content Root: {ContentRoot}", AppContext.BaseDirectory);
    logger.LogInformation("[STARTUP] Kestrel listening on: http://0.0.0.0:31457");
    logger.LogInformation("[STARTUP] File sync endpoint: POST http://localhost:31457/fileSync");
    logger.LogInformation("[STARTUP] Dashboard URL: http://localhost:31457");
    logger.LogInformation("[STARTUP] Root directory: {RootDir}", rootDir);
    logger.LogInformation("[STARTUP] Download timeout: {Timeout}s", downloadTimeout);
    logger.LogInformation("[STARTUP] Max concurrent downloads: {MaxConcurrent}",
        builder.Configuration.GetValue("FileSyncSettings:MaxConcurrentDownloads", AppConstants.MaxConcurrentDownloads));
    logger.LogInformation("========================================================");

    app.UseDefaultFiles();
    app.UseStaticFiles();

    app.Use(async (context, next) =>
    {
        context.Response.Headers["X-Content-Type-Options"] = "nosniff";
        context.Response.Headers["X-Frame-Options"] = "DENY";
        context.Response.Headers["Content-Security-Policy"] = "default-src 'self'; script-src 'self'; style-src 'self' 'unsafe-inline'";
        context.Response.Headers["X-XSS-Protection"] = "1; mode=block";
        await next();
    });

    app.MapFileSyncEndpoints();
    app.MapDashboardEndpoints();

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "FileSyncService terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
