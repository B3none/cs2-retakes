using System.Text.Json;

namespace RetakesPlugin.Modules.Configs;

public class MapConfig
{
    private readonly string _mapName;
    private readonly string _mapConfigDirectory;
    private readonly string _mapConfigPath;
    private MapConfigData? _mapConfigData;

    public MapConfig(string moduleDirectory, string mapName)
    {
        _mapName = mapName;
        _mapConfigDirectory = Path.Combine(moduleDirectory, "map_config");
        _mapConfigPath = Path.Combine(_mapConfigDirectory, $"{mapName}.json");
        _mapConfigData = null;
    }

    public void Load()
    {
        Helpers.Debug($"Attempting to load map data from {_mapConfigPath}");

        try
        {
            if (!File.Exists(_mapConfigPath))
            {
                throw new FileNotFoundException();
            }

            var jsonData = File.ReadAllText(_mapConfigPath);
            _mapConfigData = JsonSerializer.Deserialize<MapConfigData>(jsonData, Helpers.JsonSerializerOptions);

            // TODO: Implement validation to make sure the config is valid / has enough spawns.
            // if (_mapConfigData!.Spawns == null || _mapConfigData.Spawns.Count < 0)
            // {
            //     throw new Exception("No spawns found in config");
            // }

            Helpers.Debug($"Data loaded from {_mapConfigPath}");
        }
        catch (FileNotFoundException)
        {
            Helpers.Debug($"No config for map {_mapName}");
            _mapConfigData = new MapConfigData();
            Save();
        }
        catch (Exception ex)
        {
            Helpers.Debug($"An error occurred while loading data: {ex.Message}");
        }
    }

    /**
     * This function returns a clone of the spawns list. (free to mutate :>)
     */
    public List<Spawn> GetSpawnsClone()
    {
        if (_mapConfigData == null)
        {
            throw new Exception("Map config data is null");
        }

        return _mapConfigData.Spawns.ToList();
    }

    public bool AddSpawn(Spawn spawn)
    {
        _mapConfigData ??= new MapConfigData();

        // Check if the spawn already exists based on vector and bombsite
        if (_mapConfigData.Spawns.Any(existingSpawn =>
                existingSpawn.Vector == spawn.Vector && existingSpawn.Bombsite == spawn.Bombsite))
        {
            return false; // Spawn already exists, avoid duplication
        }

        _mapConfigData.Spawns.Add(spawn);

        Save();
        Load();

        return true;
    }

    public bool RemoveSpawn(Spawn spawn)
    {
        _mapConfigData ??= new MapConfigData();

        if (!_mapConfigData.Spawns.Any(existingSpawn =>
                existingSpawn.Vector == spawn.Vector && existingSpawn.Bombsite == spawn.Bombsite))
        {
            return false; // Spawn doesn't exist, avoid removing
        }

        _mapConfigData.Spawns.Remove(spawn);

        Save();
        Load();

        return true;
    }

    private MapConfigData GetSanitisedMapConfigData()
    {
        if (_mapConfigData == null)
        {
            throw new Exception("Map config data is null");
        }

        // Remove any duplicate spawns in the list
        _mapConfigData.Spawns = _mapConfigData.Spawns
            .GroupBy(spawn => new { spawn.Vector, spawn.Bombsite })
            .Select(group => group.First())
            .ToList();

        return _mapConfigData;
    }

    private void Save()
    {
        var jsonString = JsonSerializer.Serialize(GetSanitisedMapConfigData(), Helpers.JsonSerializerOptions);

        try
        {
            if (!Directory.Exists(_mapConfigDirectory))
            {
                Directory.CreateDirectory(_mapConfigDirectory);
            }

            File.WriteAllText(_mapConfigPath, jsonString);

            Helpers.Debug($"Data has been written to {_mapConfigPath}");
        }
        catch (IOException e)
        {
            Helpers.Debug($"An error occurred while writing to the file: {e.Message}");
        }
    }

    public static bool IsLoaded(MapConfig? mapConfig, string currentMap)
    {
        if (mapConfig == null || mapConfig._mapName != currentMap)
        {
            return false;
        }

        return true;
    }
}
