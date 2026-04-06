namespace winget_automatic;

using System.Diagnostics;

public class Worker(ILogger<Worker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            // Finding Winget on the System
            Console.WriteLine("INFO: Finding WinGet on the system...");
            string wingetPath = GetWingetPath();
            if (string.IsNullOrEmpty(wingetPath))
            {
                Console.WriteLine("ERROR: WinGet not found on the system.");
                return;
            }

            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = wingetPath,
                Arguments = "upgrade --all --silent --accept-source-agreements --accept-package-agreements",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            // Applying updates
            Console.WriteLine("INFO: Applying updates...");
            using (Process process = Process.Start(startInfo))
            {
                // Prints the output in real time
                string output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();

                Console.WriteLine(output);
                Console.WriteLine("INFO: Updates applied.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ERROR: Error applying updates: {ex.Message}");
        }
    }

    // Try to find Winget command line on the system
    static string? GetWingetPath()
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