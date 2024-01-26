using System.Text.Json;

namespace RetakesPlugin.Modules.Configs;

public class RetakesConfig
{
    private readonly string _retakesConfigPath;
    public RetakesConfigData? RetakesConfigData;

    public RetakesConfig(string moduleDirectory)
    {
        _retakesConfigPath = Path.Combine(moduleDirectory, "retakes_config.json");
        RetakesConfigData = null;
    }

    public void Load()
    {
        Helpers.WriteLine($"{RetakesPlugin.LogPrefix}Attempting to load data from {_retakesConfigPath}");

        try
        {
            if (!File.Exists(_retakesConfigPath))
            {
                throw new FileNotFoundException();
            }

            var jsonData = File.ReadAllText(_retakesConfigPath);
            RetakesConfigData = JsonSerializer.Deserialize<RetakesConfigData>(jsonData);

            if (RetakesConfigData == null)
            {
                throw new Exception("Retakes config is null after deserialization");
            }

            if (RetakesConfigData.Version != RetakesConfigData.CurrentVersion)
            {
                UpdateVersion();
                throw new Exception("Config is outdated");
            }

            Helpers.WriteLine($"{RetakesPlugin.LogPrefix}Data loaded from {_retakesConfigPath}");
        }
        catch (FileNotFoundException)
        {
            Helpers.WriteLine($"{RetakesPlugin.LogPrefix}No retakes config.");
            RetakesConfigData = new RetakesConfigData();
            Save();
        }
        catch (Exception ex)
        {
            Helpers.WriteLine($"{RetakesPlugin.LogPrefix}An error occurred while loading data: {ex.Message}");
        }
    }

    private void Save()
    {
        var jsonString = JsonSerializer.Serialize(RetakesConfigData, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        try
        {
            File.WriteAllText(_retakesConfigPath, jsonString);

            Helpers.WriteLine($"{RetakesPlugin.LogPrefix}Data has been written to {_retakesConfigPath}");
        }
        catch (IOException e)
        {
            Helpers.WriteLine($"{RetakesPlugin.LogPrefix}An error occurred while writing to the file: {e.Message}");
        }
    }

    public static bool IsLoaded(RetakesConfig? retakesConfig)
    {
        if (retakesConfig == null)
        {
            Helpers.WriteLine($"{RetakesPlugin.LogPrefix}Retakes config is null");
            return false;
        }

        if (retakesConfig.RetakesConfigData == null)
        {
            Helpers.WriteLine($"{RetakesPlugin.LogPrefix}Retakes config data is null");
            return false;
        }

        return true;
    }

    private bool UpdateVersion()
    {
        if (RetakesConfigData == null)
        {
            return false;
        }

        if (RetakesConfigData.Version == RetakesConfigData.CurrentVersion)
        {
            return true;
        }

        RetakesConfigData.Version = RetakesConfigData.CurrentVersion;
        Save();

        return true;
    }
}