using Serilog;
using WinCCReportingWorkerMarefa;
using WinCCReportingWorkerMarefa.Models;
using WinCCReportingWorkerMarefa.Services;

var builder = Host.CreateApplicationBuilder(args);

// ============================================
// SERILOG
// ============================================

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File(
        Path.Combine(
            AppContext.BaseDirectory,
            "Logs",
            "worker-.log"),
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 30)
    .CreateLogger();

builder.Services.AddSerilog();

// ============================================
// WINCC CONFIGURATION
// ============================================

builder.Services.Configure<WinCCOptions>(
    builder.Configuration.GetSection("WinCC"));

builder.Services.Configure<SyncSettings>(
    builder.Configuration.GetSection("SyncSettings"));

// ============================================
// HTTP CLIENT
// ============================================

builder.Services.AddHttpClient<GraphQLService>();

// ============================================
// SERVICES
// ============================================

builder.Services.AddSingleton<RedundancyService>();

builder.Services.AddSingleton<SqlService>();

builder.Services.AddSingleton<AlarmDatabaseService>();

builder.Services.AddSingleton<AlarmService>();

// ============================================
// BACKGROUND WORKER
// ============================================

builder.Services.AddHostedService<Worker>();

// ============================================
// WINDOWS SERVICE
// ============================================

builder.Services.AddWindowsService(options =>
{
    options.ServiceName = "WinCC Reporting Worker";
});



// ============================================
// BUILD + RUN
// ============================================

var host = builder.Build();

try
{
    await host.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "APPLICATION FAILED");
}
finally
{
    await Log.CloseAndFlushAsync();
}