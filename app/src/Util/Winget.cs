namespace WingetAutomatic.Util;

using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using WingetAutomatic.Exception;
using WingetAutomatic.Model;
using WingetAutomatic.Repository;
using Microsoft.Management.Deployment;

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
        var tcs = new TaskCompletionSource<List<string>>();

        // O WinGet exige o modelo STA. Serviços rodam em MTA, logo criamos uma thread dedicada.
        var staThread = new Thread(() =>
        {
            try
            {
                // Lista de pacotes desatualizados
                var outdatedPackages = new List<string>();

                // Inicializando o PackageManager do WinGet
                var packageManager = new PackageManager();

                // Obtendo os catálogos de pacotes disponíveis
                var remoteCatalogs = packageManager.GetPackageCatalogs();

                // Criando um catálogo composto para correlacionar os pacotes locais com as fontes remotas
                var compositeOptions = new CreateCompositePackageCatalogOptions
                {
                    CompositeSearchBehavior = CompositeSearchBehavior.LocalCatalogs
                };

                // Adicionando o catálogo local de instalados
                var localCatalogRef = packageManager.GetLocalPackageCatalog(LocalPackageCatalog.InstalledPackages);
                compositeOptions.Catalogs.Add(localCatalogRef);

                // Adicionando os catálogos remotos disponíveis para cruzar os dados de versão
                foreach (var catalogRef in remoteCatalogs)
                {
                    compositeOptions.Catalogs.Add(catalogRef);
                }

                // Conectando ao catálogo composto
                var compositeCatalogRef = packageManager.CreateCompositePackageCatalog(compositeOptions);
                var connectResult = compositeCatalogRef.Connect();

                if (connectResult.Status != ConnectResultStatus.Ok || connectResult.PackageCatalog == null)
                {
                    throw new InvalidOperationException($"Falha ao conectar ao catálogo do WinGet: {connectResult.Status}.");
                }

                var catalog = connectResult.PackageCatalog;

                // Configurando as opções de busca para varrer os pacotes
                var findOptions = new FindPackagesOptions();

                var filter = new PackageMatchFilter
                {
                    Field = PackageMatchField.Id,
                    Option = PackageFieldMatchOption.ContainsCaseInsensitive,
                    Value = string.Empty // Traz todos os pacotes para validação de atualização
                };
                findOptions.Filters.Add(filter);

                stoppingToken.ThrowIfCancellationRequested();

                // Executando a busca
                var findResult = catalog.FindPackages(findOptions);

                if (findResult.Status == FindPackagesResultStatus.Ok && findResult.Matches != null)
                {
                    // Filtrando os pacotes que possuem uma versão disponível para atualização
                    foreach (var match in findResult.Matches)
                    {
                        stoppingToken.ThrowIfCancellationRequested();

                        var package = match.CatalogPackage;

                        if (package.InstalledVersion is not null && package.IsUpdateAvailable)
                        {
                            outdatedPackages.Add(package.Id);
                        }
                    }

                    tcs.SetResult(outdatedPackages);
                }
            }

            catch (OperationCanceledException)
            {
                tcs.SetCanceled();
            }
            catch (System.Exception ex)
            {
                tcs.SetException(ex);
            }
        });

        // Configura a thread como STA antes de iniciar
        staThread.SetApartmentState(ApartmentState.STA);
        staThread.Start();

        return await tcs.Task;
    }

    public async Task UpdatePackageAsync(string packageName, CancellationToken stoppingToken)
    {
        StringBuilder args = new StringBuilder();
        args.Append("update ");
        args.Append($"{packageName} ");

        if (configuration.rebootPolicy == RebootPolicy.WhenNecessary || configuration.rebootPolicy == RebootPolicy.Always)
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