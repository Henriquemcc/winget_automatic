using WingetAutomatic.Model;
using WingetAutomatic.Repository;
using System.Text.Json;

class ConfigurationRepositoryImpl : ConfigurationRepository
{
    private string configFolder;
    private string configFile;

    public ConfigurationRepositoryImpl()
    {
        configFolder = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "WingetAutomatic");
        configFile = Path.Join(configFolder, "config.json");
    }

    public Configuration? load()
    {
        StreamReader streamReader = new StreamReader(configFile);
        string jsonString = streamReader.ReadToEnd();
        streamReader.Close();
        return JsonSerializer.Deserialize<Configuration>(jsonString);
    }

    public void save(Configuration configuration)
    {
        string jsonString = JsonSerializer.Serialize(configuration);
        StreamWriter streamWriter = new StreamWriter(configFile);
        streamWriter.Write(jsonString);
        streamWriter.Close();
    }
}