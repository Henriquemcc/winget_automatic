namespace WingetAutomatic;

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
                logger.LogInformation("Getting updates...");
                List<string> outdatedPackages = await winget.GetOutdatedPackagesAsync(stoppingToken);

                // Removing ignored packages
                foreach (string ignoredPackage in configuration.ignoredPackages)
                {
                    outdatedPackages.Remove(ignoredPackage);
                }

                // Updating packages
                foreach (string outdatedPackage in outdatedPackages)
                {
                    // Stopping loop if system is shutting down
                    if (stoppingToken.IsCancellationRequested) break;

                    logger.LogInformation("Updating {0}...", outdatedPackage);

                    // We use CancellationToken.None here so that, if the shutdown starts
                    // now, this specific command finishes before we close the application.
                    await winget.UpdatePackageAsync(outdatedPackage, CancellationToken.None);
                }

                // Saving last update
                saveLastUpdate(true, DateTime.Now);

                if (!stoppingToken.IsCancellationRequested)
                    logger.LogInformation("Updates applied.");

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

            // Important: The service must wait a period of time, otherwise it will run in infinite loop consuming CPU
            // or will end immediately, which would cause error in the MSI.
            await Task.Delay(configuration.updateInterval, stoppingToken);
        }
    }

    private void saveLastUpdate(bool success, DateTime dateTime)
    {
        lastUpdate.success = success;
        lastUpdate.dateTime = dateTime;
        lastUpdateRepository.save(lastUpdate);
    }
}