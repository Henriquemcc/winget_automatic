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

        var args = new StringBuilder("upgrade ");
        if (configuration.downloadProxy != null)
        {
            args.Append("--proxy ").Append(configuration.downloadProxy).Append(' ');
        }
        if (configuration.disableProxy)
        {
            args.Append("--no-proxy ");
        }

        string output = await RunWingetCommandAsync(args.ToString(), stoppingToken);

        string[] lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

        // 1) Acha a linha separadora (uma sequência de '-') e o CABEÇALHO acima dela.
        //    Isto é independente de idioma e NÃO confunde linhas de dados que contêm "id"
        //    como substring — caso do código original, onde "IntelliJ IDEA" (IDEA) e
        //    "NVIDIA ..." (NVIDIA) eram tratados como cabeçalho, descartados, e ainda
        //    corrompiam a coluna `left` para todas as linhas seguintes.
        int separatorIdx = -1;
        for (int i = 0; i < lines.Length; i++)
        {
            string trimmed = lines[i].Trim();
            if (trimmed.Length > 0 && trimmed.All(c => c == '-'))
            {
                separatorIdx = i;
                break;
            }
        }
        if (separatorIdx <= 0) return packageIds;

        string header = lines[separatorIdx - 1];
        int left = header.IndexOf("ID", StringComparison.Ordinal); // "ID" é o cabeçalho da coluna em qualquer idioma
        if (left < 0) return packageIds;

        // 2) Token de ID: aceita hífen, '+', '_' e '.'.
        //    O regex original [a-zA-Z0-9]+(\.[a-zA-Z0-9]+)+ cortava em:
        //      Adobe.Acrobat.Reader.64-bit -> "Adobe.Acrobat.Reader.64"
        //      yt-dlp.FFmpeg               -> "dlp.FFmpeg"        (e ainda deslocado)
        //      RedHat.Podman-Desktop       -> "RedHat.Podman"
        //      Microsoft.VCRedist.2015+.x64-> "Microsoft.VCRedist.2015"
        var idTokenRegex = new Regex(@"^[A-Za-z0-9][A-Za-z0-9.\-+_]*$");

        for (int i = separatorIdx + 1; i < lines.Length; i++)
        {
            string line = lines[i];
            if (string.IsNullOrWhiteSpace(line)) continue;

            // Fim da tabela: linha de resumo ("40 atualizações disponíveis.") ou o
            // bloco "exige segmentação explícita" (Discord). Nesses casos o token na
            // coluna do ID não casa o padrão -> paramos de ler esta tabela.
            if (left >= line.Length) break;

            string rest = line.Substring(left).TrimStart();
            int space = rest.IndexOfAny(new[] { ' ', '\t' });
            string token = space < 0 ? rest : rest.Substring(0, space);

            if (!token.Contains('.') || !idTokenRegex.IsMatch(token))
                break; // não é mais uma linha de dados da tabela principal

            // 3) IDs truncados pelo winget com "…" (U+2026). O texto não contém o Id
            //    completo, então NÃO há como recuperá-lo por parse. Pular evita gerar
            //    comandos "winget upgrade <id-incompleto>" que sempre falhariam.
            //    Para obter estes pacotes corretamente use a API COM (abaixo).
            if (token.EndsWith('…'))
            {
                // TODO(registrar): logar token truncado para diagnóstico.
                continue;
            }

            packageIds.Add(token);
        }

        return packageIds;
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