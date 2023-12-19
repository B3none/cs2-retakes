using System.Text.Json;

namespace RetakesPlugin.Modules.Config;

public class MapConfig
{
    public readonly string MapName;
    private readonly string _mapConfigPath;
    private MapConfigData? _mapConfigData;
    
    public MapConfig(string moduleDirectory, string mapName)
    {
        MapName = mapName;
        _mapConfigPath = Path.Combine(moduleDirectory, mapName, ".json");
        _mapConfigData = null;
    }

    public void Load()
    {
        try
        {
            if (!File.Exists(_mapConfigPath))
            {
                throw new FileNotFoundException("Map configuration file not found.");
            }

            string jsonData = File.ReadAllText(_mapConfigPath);
            _mapConfigData = JsonSerializer.Deserialize<MapConfigData>(jsonData);
            Console.WriteLine($"{RetakesPlugin.MessagePrefix}Data loaded from {_mapConfigPath}");
        }
        catch (FileNotFoundException ex)
        {
            Console.WriteLine($"{RetakesPlugin.MessagePrefix}Error loading data: {ex.Message}");
            throw; // Re-throw the exception to handle it in the calling code
        }
        catch (Exception ex)
        {
            Console.WriteLine($"{RetakesPlugin.MessagePrefix}An error occurred while loading data: {ex.Message}");
        }
    }

    public void Save()
    {
        // Convert object to JSON string
        string jsonString = JsonSerializer.Serialize(_mapConfigData);

        try
        {
            // Write JSON string to the file
            File.WriteAllText(_mapConfigPath, jsonString);

            Console.WriteLine(RetakesPlugin.MessagePrefix + "Data has been written to " + _mapConfigPath);
        }
        catch (IOException e)
        {
            Console.WriteLine($"{RetakesPlugin.MessagePrefix}An error occurred while writing to the file: {e.Message}");
        }
    }
}
