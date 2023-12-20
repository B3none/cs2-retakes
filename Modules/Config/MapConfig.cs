using System.Text.Json;

namespace RetakesPlugin.Modules.Config;

public class MapConfig
{
    public readonly string MapName;
    private readonly string _mapConfigDirectory;
    private readonly string _mapConfigPath;
    private MapConfigData? _mapConfigData;
    
    public MapConfig(string moduleDirectory, string mapName)
    {
        MapName = mapName;
        _mapConfigDirectory = Path.Combine(moduleDirectory, "map_config");
        _mapConfigPath = Path.Combine(_mapConfigDirectory, $"{mapName}.json");
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

            var jsonData = File.ReadAllText(_mapConfigPath);
            _mapConfigData = JsonSerializer.Deserialize<MapConfigData>(jsonData);

            // TODO: Implement validation to make sure the config is valid / has enough spawns.
            // if (_mapConfigData!.Spawns == null || _mapConfigData.Spawns.Count < 0)
            // {
            //     throw new Exception("No spawns found in config");
            // }
            
            Console.WriteLine($"{RetakesPlugin.MessagePrefix}Data loaded from {_mapConfigPath}");
        }
        catch (FileNotFoundException)
        {
            Console.WriteLine($"{RetakesPlugin.MessagePrefix}No config for map {MapName}");
            _mapConfigData = new MapConfigData();
            Save();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"{RetakesPlugin.MessagePrefix}An error occurred while loading data: {ex.Message}");
        }
    }

    public void AddSpawn(Spawn spawn)
    {
        _mapConfigData ??= new MapConfigData();
        
        _mapConfigData.Spawns.Add(spawn);
        
        Save();
    }

    public void RemoveSpawn()
    {
        // TODO: Implement this.
    }

    private void Save()
    {
        // Convert object to JSON string
        var jsonString = JsonSerializer.Serialize(_mapConfigData);

        try
        {
            if (!Directory.Exists(_mapConfigDirectory))
            {
                Directory.CreateDirectory(_mapConfigDirectory);
            }
            
            // Write JSON string to the file
            File.WriteAllText(_mapConfigPath, jsonString);

            Console.WriteLine($"{RetakesPlugin.MessagePrefix}Data has been written to " + _mapConfigPath);
        }
        catch (IOException e)
        {
            Console.WriteLine($"{RetakesPlugin.MessagePrefix}An error occurred while writing to the file: {e.Message}");
        }
    }
}
