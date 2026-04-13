namespace WingetAutomatic;

using System.Net.Http;
using System.Diagnostics;
using WingetAutomatic.Model;
using WingetAutomatic.Repository;
using WingetAutomatic.Util;

public class Worker : BackgroundService
{
    Configuration configuration;
    LastUpdate lastUpdate;
    ILogger<Worker> logger;
    Winget winget;
    ConfigurationRepository configurationRepository;
    LastUpdateRepository lastUpdateRepository;

    public Worker(ILogger<Worker> logger, Winget winget, ConfigurationRepository configurationRepository, LastUpdateRepository lastUpdateRepository)
    {
        this.logger = logger;
        this.winget = winget;
        this.configurationRepository = configurationRepository;
        this.lastUpdateRepository = lastUpdateRepository;

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

        // Getting last update
        LastUpdate? lastUpdate = lastUpdateRepository.load();
        if (lastUpdate == null)
        {
            this.lastUpdate = new LastUpdate();
            lastUpdateRepository.save(this.lastUpdate);
        }
        else
        {
            this.lastUpdate = lastUpdate;
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Delaying update until it has passed the update time interval
                if (lastUpdate != null && lastUpdate.dateTime != null && lastUpdate.success == true)
                {
                    DateTime timeDistance = (lastUpdate.dateTime ?? DateTime.MinValue) + configuration.updateInterval;
                    if (DateTime.Now < timeDistance)
                    {
                        await Task.Delay(timeDistance - DateTime.Now, stoppingToken);
                    }
                }

                // Waiting for Internet Connection
                while (!await IsInternetAvailableAsync(stoppingToken))
                {
                    logger.LogInformation("No connection to the Internet. Waiting 1 minute to try again...");
                    await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
                }

                logger.LogInformation("Getting updates...");
                List<string> outdatedPackages = await winget.GetOutdatedPackagesAsync(stoppingToken);
                logger.LogInformation("Found {0} outdated packages.", outdatedPackages.Count);

                // Removing ignored packages
                foreach (string ignoredPackage in configuration.ignoredPackages)
                {
                    outdatedPackages.Remove(ignoredPackage);
                }

                // Updating packages
                lastUpdate!.packages = new List<string>();
                foreach (string outdatedPackage in outdatedPackages)
                {
                    // Stopping loop if system is shutting down
                    if (stoppingToken.IsCancellationRequested) break;

                    logger.LogInformation("Updating {0}...", outdatedPackage);

                    // We use CancellationToken.None here so that, if the shutdown starts
                    // now, this specific command finishes before we close the application.
                    await winget.UpdatePackageAsync(outdatedPackage, CancellationToken.None);

                    // Adding package to the list of installed packages
                    lastUpdate.packages.Add(outdatedPackage);
                }

                if (!stoppingToken.IsCancellationRequested)
                {
                    // Saving last update only if it wasn't interrupted
                    saveLastUpdate(true, DateTime.Now);

                    logger.LogInformation("Updates applied.");
                }          

                // Rebooting system if reboot policy is always and updates have been applied
                if (configuration.rebootPolicy == RebootPolicy.Always && outdatedPackages.Count > 0)
                {
                    logger.LogInformation("Reboot policy set to Always. Rebooting system...");
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "shutdown",
                        Arguments = "/r /t 0 /f",
                        CreateNoWindow = true,
                        UseShellExecute = false
                    });
                }

            }
            catch (System.Exception ex)
            {
                logger.LogError(ex, "Error applying updates.");
                saveLastUpdate(false, DateTime.Now);
            }
        }
    }

    private void saveLastUpdate(bool success, DateTime dateTime)
    {
        lastUpdate.success = success;
        lastUpdate.dateTime = dateTime;
        lastUpdateRepository.save(lastUpdate);
    }

    private async Task<bool> IsInternetAvailableAsync(CancellationToken ct)
    {
        try
        {
            using var client = new HttpClient();
            client.Timeout = TimeSpan.FromSeconds(5);
            using var response = await client.GetAsync("http://www.msftconnecttest.com/connecttest.txt", ct);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }
}