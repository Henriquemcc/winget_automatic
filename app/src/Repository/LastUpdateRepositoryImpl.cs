namespace WingetAutomatic.Repository;

using WingetAutomatic.Model;
using System.Text.Json;

public class LastUpdateRepositoryImpl : LastUpdateRepository
{
    private string configFolder;
    private string lastUpdateFile;

    public LastUpdateRepositoryImpl()
    {
        configFolder = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "WingetAutomatic");

        // Creating config directory
        if (!Directory.Exists(configFolder))
            Directory.CreateDirectory(configFolder);

        lastUpdateFile = Path.Join(configFolder, "lastUpdate.json");
    }

    public LastUpdate? load()
    {
        if (!File.Exists(lastUpdateFile)) return null;

        try
        {
            string jsonString = File.ReadAllText(lastUpdateFile);
            return string.IsNullOrWhiteSpace(jsonString)
                ? null
                : JsonSerializer.Deserialize<LastUpdate>(jsonString);
        }
        catch { return null; }
    }

    public void save(LastUpdate lastUpdate)
    {
        var options = new JsonSerializerOptions { WriteIndented = true, IncludeFields = true };
        string jsonString = JsonSerializer.Serialize(lastUpdate, options);
        File.WriteAllText(lastUpdateFile, jsonString);
    }
}