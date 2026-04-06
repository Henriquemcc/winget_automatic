namespace winget_automatic;

using System.Diagnostics;

public class Worker(ILogger<Worker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                logger.LogInformation("Finding WinGet on the system...");
                string? wingetPath = GetWingetPath();
                
                if (string.IsNullOrEmpty(wingetPath))
                {
                    logger.LogError("WinGet not found on the system.");
                }
                else
                {
                    ProcessStartInfo startInfo = new ProcessStartInfo
                    {
                        FileName = wingetPath,
                        Arguments = "upgrade --all --silent --accept-source-agreements --accept-package-agreements",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };

                    logger.LogInformation("Applying updates...");
                    using (Process? process = Process.Start(startInfo))
                    {
                        if (process != null)
                        {
                            // Using Task to avoid blocking the service main thread
                            string output = await process.StandardOutput.ReadToEndAsync(stoppingToken);
                            await process.WaitForExitAsync(stoppingToken);

                            logger.LogInformation("Winget Output: {Output}", output);
                            logger.LogInformation("Updates applied.");
                        }
                    }
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

    // Try to find Winget command line on the system
    private string? GetWingetPath()
    {
        string? wingetPath = null;

        // Trying to find Winget on System PATH first
        wingetPath = Environment.GetEnvironmentVariable("PATH")?
            .Split(';')
            .Select(p => Path.Combine(p, "winget.exe"))
            .FirstOrDefault(File.Exists);

        // Searching on the folder WindowsApps
        if (wingetPath == null)
        {
            string windowsAppsPath = @"C:\Program Files\WindowsApps";
            if (Directory.Exists(windowsAppsPath))
            {
                var files = Directory.GetFiles(windowsAppsPath, "winget.exe", SearchOption.AllDirectories);
                wingetPath =  files.First();
            }
        }

        return wingetPath;
    }
}