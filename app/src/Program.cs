using WingetAutomatic;
using Microsoft.Extensions.Logging.Configuration;
using Microsoft.Extensions.Logging.EventLog;
using WingetAutomatic.Model;
using WingetAutomatic.Util;
using WingetAutomatic.Repository;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.Configure<HostOptions>(options =>
{
    // Set a longer timeout for graceful shutdown (e.g., 10 minutes).
    options.ShutdownTimeout = TimeSpan.FromMinutes(10);
});

builder.Services.AddSingleton<Winget>();
builder.Services.AddSingleton<ConfigurationRepository, ConfigurationRepositoryImpl>();
builder.Services.AddSingleton<LastUpdateRepository, LastUpdateRepositoryImpl>();

builder.Services.AddWindowsService(options =>
{
    options.ServiceName = "WingetAutomaticService";
});

if (OperatingSystem.IsWindows())
{
    LoggerProviderOptions.RegisterProviderOptions<
        EventLogSettings, EventLogLoggerProvider>(builder.Services);
}

builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
