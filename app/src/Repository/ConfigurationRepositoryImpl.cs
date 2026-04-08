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
        if (!File.Exists(configFile))
            return null;

        try
        {
            using (StreamReader streamReader = new StreamReader(configFile))
            {
                string jsonString = streamReader.ReadToEnd();
                return JsonSerializer.Deserialize<Configuration>(jsonString);
            }
        }
        catch (JsonException)
        {
            // If the JSON is malformed, treat it as null to create a new one.
            return null;
        }
    }

    public void save(Configuration configuration)
    {
        string jsonString = JsonSerializer.Serialize(configuration);
        using (StreamWriter streamWriter = new StreamWriter(configFile))
        {
            streamWriter.Write(jsonString);
        }
    }
}