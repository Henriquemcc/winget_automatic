using WingetAutomatic;
using Microsoft.Extensions.Logging.Configuration;
using Microsoft.Extensions.Logging.EventLog;

var builder = Host.CreateApplicationBuilder(args);
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
