namespace WingetAutomatic.Util;

using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using WingetAutomatic.Exception;
using WingetAutomatic.Model;
using WingetAutomatic.Repository;

public class Winget
{
    private string wingetPath;
    private ConfigurationRepository configurationRepository;
    private Configuration configuration;
    private ILogger<Worker> logger;

    public Winget(ConfigurationRepository configurationRepository, ILogger<Worker> logger)
    {
        this.logger = logger;
        this.configurationRepository = configurationRepository;
        
        // Getting configuration
        Configuration? configuration = configurationRepository.load();
        if (configuration == null)
        {
            this.configuration = new Configuration();
            configurationRepository.save(this.configuration);
        }
        else
        {
            this.configuration = configuration;
        }

        // Getting WinGet path
        string? wingetPath = GetWingetPath();
        if (string.IsNullOrEmpty(wingetPath))
        {
            logger.LogError("WinGet not found on the system.");
            throw new WingetNotFoundException();
        }
        else
        {
            this.wingetPath = wingetPath;
        }
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

        int idLeft = 0;
        foreach (string line in lines)
        {
            if (line.Contains("ID"))
            {
                idLeft = line.IndexOf("ID");
                continue;
            }

            if (line.Contains("---"))
            {
                isTableData = true;
                continue;
            }

            if (isTableData)
            {
                int idRight = line.IndexOf(" ", idLeft);
                packageIds.Add(line.Substring(idLeft, idRight - idLeft));
                isTableData = false;
            }
        }

        return packageIds;
    }

    public async Task UpdatePackageAsync(string packageName, CancellationToken stoppingToken)
    {
        StringBuilder args = new StringBuilder();
        args.Append("update ");
        args.Append($"{packageName} ");

        if (configuration.rebootPolicy == Models.RebootPolicy.WhenNecessary || configuration.rebootPolicy == Models.RebootPolicy.Always)
        {
            args.Append("--allow-reboot ");
        }

        if (configuration.ignoreSecurityHash)
        {
            args.Append("--ignore-security-hash ");
        }

        if (configuration.ignoreMalwareScan)
        {
            args.Append("--ignore-malware-scan ");
        }

        if (configuration.downloadProxy != null)
        {
            args.Append("--proxy ");
            args.Append(configuration.downloadProxy);
        }

        if (configuration.disableProxy)
        {
            args.Append("--no-proxy ");
        }

        await RunWingetCommandAsync(args.ToString(), stoppingToken);
    }
}