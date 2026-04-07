namespace WingetAutomatic.Util;

using System.Diagnostics;
using System.Text.RegularExpressions;
using WingetAutomatic.Model;

public class Winget
{
    private string? wingetPath;
    private Configuration configuration;

    public Winget(Configuration configuration)
    {
        this.configuration = configuration;
        wingetPath = GetWingetPath();
    }


    // Try to find Winget command line on the system
    public string? GetWingetPath()
    {
        string? wingetPath = null;

        // Trying to find Winget on System PATH first
        wingetPath = Environment.GetEnvironmentVariable("PATH")?
            .Split(';')
            .Select(p => Path.Combine(p, "winget.exe"))
            .FirstOrDefault(File.Exists);

        // Searching on the folder WindowsApps (requires caution with permissions)
        if (wingetPath == null)
        {
            string windowsAppsPath = @"C:\Program Files\WindowsApps";
            if (Directory.Exists(windowsAppsPath))
            {
                var files = Directory.GetFiles(windowsAppsPath, "winget.exe", SearchOption.AllDirectories);
                wingetPath = files.FirstOrDefault();
            }

        }

        return wingetPath;
    }

    public async Task<string> RunWingetCommandAsync(string arguments, CancellationToken stoppingToken)
    {
        ProcessStartInfo startInfo = new ProcessStartInfo
        {
            FileName = wingetPath,
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using (Process? process = Process.Start(startInfo))
        {
            if (process == null) return "Failed to start WinGet process.";

            string output = await process.StandardOutput.ReadToEndAsync(stoppingToken);
            await process.WaitForExitAsync(stoppingToken);
            return output;
        }
    }

    // Obtains outdated packages using WinGet
    public async Task<List<string>> GetOutdatedPackagesAsync(CancellationToken stoppingToken)
    {
        var packageIds = new List<string>();
        if (string.IsNullOrEmpty(wingetPath)) return packageIds;

        string output = await RunWingetCommandAsync("upgrade", stoppingToken);

        var lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        bool isTableData = false;

        foreach (var line in lines)
        {
            if (line.Contains("---"))
            {
                isTableData = true;
                continue;
            }

            if (isTableData)
            {
                // WinGet separates columns with 2 or more spaces. The ID is the second column.
                var columns = Regex.Split(line.Trim(), @"\s{2,}");
                if (columns.Length >= 2)
                {
                    packageIds.Add(columns[0].Split(" ")[2]);
                }
            }
        }

        return packageIds;
    }
}