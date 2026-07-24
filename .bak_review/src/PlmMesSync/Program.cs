using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using PlmMesSync;
using PlmMesSync.Data;
using PlmMesSync.Endpoints;
using PlmMesSync.Logging;
using PlmMesSync.Services;
using Serilog;

using System;
using System.IO;

if (args.Length >= 2 && args[0] == "--encrypt")
{
    var encrypted = ConfigCipher.Encrypt(args[1]);
    Console.WriteLine($"ENC({encrypted})");
    return;
}

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
    builder.Host.UseWindowsService(options => options.ServiceName = "PLM_MES_SAP_Ponto");

    builder.WebHost.ConfigureKestrel(k => k.ListenAnyIP(31456));

    builder.Host.UseSerilog((context, configuration) =>
        configuration.ReadFrom.Configuration(context.Configuration));

    var connStr = ConfigCipher.DecryptConnectionString(
        builder.Configuration.GetConnectionString("AgileDb")!);

    builder.Services.AddDbContext<AgileDbContext>(opts =>
        opts.UseOracle(connStr)
            .EnableSensitiveDataLogging()
            .EnableDetailedErrors());

    builder.Services.AddSingleton<TriggerStore>();
    builder.Services.AddSingleton<SyncStore>();
    builder.Services.AddSingleton<BomFileLogger>();
    builder.Services.AddHttpClient<MesUploadService>();
    builder.Services.AddScoped<IBomSyncService, BomSyncService>();
    builder.Services.AddSingleton<BomDebouncerService>();
    builder.Services.AddHostedService(sp => sp.GetRequiredService<BomDebouncerService>());

    builder.Services.AddSingleton<FileSyncDebouncerService>();
    builder.Services.AddHostedService(sp => sp.GetRequiredService<FileSyncDebouncerService>());
    builder.Services.AddHttpClient<FileSyncUploadService>();
    builder.Services.AddHttpClient("FileSyncDashboard", client =>
    {
        var baseUrl = builder.Configuration.GetValue<string>("FileSyncSettings:FileSyncDashboardUrl")
            ?? "http://10.170.9.4:31457";
        client.BaseAddress = new Uri(baseUrl);
        client.Timeout = TimeSpan.FromSeconds(10);
    });

    var app = builder.Build();

    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    logger.LogInformation("========================================================");
    logger.LogInformation("[STARTUP] PLM_MES_SAP_Ponto Service starting");
    logger.LogInformation("[STARTUP] Content Root: {ContentRoot}", AppContext.BaseDirectory);
    logger.LogInformation("[STARTUP] Kestrel listening on: http://0.0.0.0:31456");
    logger.LogInformation("[STARTUP] Sync delay: {Delay}s",
        builder.Configuration.GetValue("SyncSettings:DelaySeconds", 15));
    logger.LogInformation("[STARTUP] Log base path: {LogPath}",
        Path.Combine(AppContext.BaseDirectory, builder.Configuration["SyncSettings:LogBasePath"] ?? "log"));

    var maskedConn = System.Text.RegularExpressions.Regex.Replace(
        connStr, @"Password=[^;]+", "Password=***");
    logger.LogInformation("[STARTUP] Oracle connection: {ConnStr}", maskedConn);

    logger.LogInformation("[STARTUP] Testing database connection...");
    try
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AgileDbContext>();
        var canConnect = await db.Database.CanConnectAsync();
        if (canConnect)
            logger.LogInformation("[STARTUP] Database connection SUCCESS");
        else
            logger.LogError("[STARTUP] Database connection FAILED - CanConnect returned false");
    }
    catch (Exception dbEx)
    {
        logger.LogError(dbEx, "[STARTUP] Database connection FAILED - {Error}", dbEx.Message);
    }

    logger.LogInformation("[STARTUP] Dashboard URL: http://localhost:31456");
    logger.LogInformation("[STARTUP] Trigger API: POST http://localhost:31456/api/trigger");
    logger.LogInformation("========================================================");

    app.UseDefaultFiles();
    app.UseStaticFiles();

    app.MapTriggerEndpoints();
    app.MapDashboardEndpoints();
    app.MapFileSyncProxyEndpoints();

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
