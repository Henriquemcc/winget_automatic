namespace WingetAutomatic;

using System.Diagnostics;
using System.Text.RegularExpressions;
using WingetAutomatic.Util;

public class Worker(ILogger<Worker> logger, Winget winget) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                logger.LogInformation("Finding WinGet on the system...");
                string? wingetPath = winget.GetWingetPath();

                if (string.IsNullOrEmpty(wingetPath))
                {
                    logger.LogError("WinGet not found on the system.");
                }
                else
                {
                    logger.LogInformation("Applying updates...");
                    string output = await winget.RunWingetCommandAsync(wingetPath, "upgrade --all --silent --accept-source-agreements --accept-package-agreements", stoppingToken);

                    logger.LogInformation("Winget Output: {Output}", output);
                    logger.LogInformation("Updates applied.");
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error applying updates.");
            }

            // Important: The service must wait a period of time, otherwise it will run in infinite loop consuming CPU
            // or will end immediately, which would cause error in the MSI.
            await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
        }
    }
}