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
        _mapConfigPath = Path.Combine(moduleDirectory, $"map_configs/{mapName}.json");
        _mapConfigData = null;
    }

    public void Load()
    {
        Console.WriteLine($"{RetakesPlugin.MessagePrefix}Attempting to load data from {_mapConfigPath}");
        
        try
        {
            if (!File.Exists(_mapConfigPath))
            {
                throw new FileNotFoundException();
            }

            string jsonData = File.ReadAllText(_mapConfigPath);
            _mapConfigData = JsonSerializer.Deserialize<MapConfigData>(jsonData);

            if (_mapConfigData!.Spawns == null || _mapConfigData.Spawns.Count == 0)
            {
                throw new Exception("No spawns found in config");
            }
            
            Console.WriteLine($"{RetakesPlugin.MessagePrefix}Data loaded from {_mapConfigPath}");
        }
        catch (FileNotFoundException)
        {
            Console.WriteLine($"{RetakesPlugin.MessagePrefix}No config for map {MapName}");
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
