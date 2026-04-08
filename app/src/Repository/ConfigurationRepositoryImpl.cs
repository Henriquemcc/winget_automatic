namespace WingetAutomatic.Repository;

using WingetAutomatic.Model;
using System.Text.Json;

public class ConfigurationRepositoryImpl : ConfigurationRepository
{
    private string configFolder;
    private string configFile;

    public ConfigurationRepositoryImpl()
    {
        configFolder = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "WingetAutomatic");

        // Creating config directory
        if (!Directory.Exists(configFolder))
            Directory.CreateDirectory(configFolder);

        configFile = Path.Join(configFolder, "config.json");
    }

    public Configuration? load()
    {
        if (!File.Exists(configFile)) return null;

        try
        {
            string jsonString = File.ReadAllText(configFile);
            return string.IsNullOrWhiteSpace(jsonString)
                ? null
                : JsonSerializer.Deserialize<Configuration>(jsonString);
        }
        catch { return null; }
    }

    public void save(Configuration configuration)
    {
        var options = new JsonSerializerOptions { WriteIndented = true, IncludeFields = true };
        string jsonString = JsonSerializer.Serialize(configuration, options);
        File.WriteAllText(configFile, jsonString);
    }
}