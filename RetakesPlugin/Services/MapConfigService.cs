using System.Text.Json;

using RetakesPlugin.Models;
using RetakesPlugin.Utils;

namespace RetakesPlugin.Services;

public class MapConfigService
{
    private readonly string _mapName;
    private readonly string _mapConfigDirectory;
    private readonly string _mapConfigPath;
    private MapConfigData? _mapConfigData;
    private readonly JsonSerializerOptions _jsonOptions;

    public MapConfigService(string moduleDirectory, string mapName, JsonSerializerOptions jsonOptions)
    {
        _mapName = mapName;
        _mapConfigDirectory = Path.Combine(moduleDirectory, "map_config");
        _mapConfigPath = Path.Combine(_mapConfigDirectory, $"{mapName}.json");
        _mapConfigData = null;
        _jsonOptions = jsonOptions;
    }

    public void Load(bool isViaCommand = false)
    {
        Logger.LogDebug("MapConfig", $"Attempting to load map data from {_mapConfigPath}");

        try
        {
            if (!File.Exists(_mapConfigPath))
            {
                throw new FileNotFoundException();
            }

            var jsonData = File.ReadAllText(_mapConfigPath);
            _mapConfigData = JsonSerializer.Deserialize<MapConfigData>(jsonData, _jsonOptions);

            Logger.LogInfo("MapConfig", $"Map config loaded for {_mapName}");
        }
        catch (FileNotFoundException)
        {
            Logger.LogWarning("MapConfig", $"No config found for map {_mapName}");

            if (!isViaCommand)
            {
                _mapConfigData = new MapConfigData();
                Save();
            }
        }
        catch (Exception ex)
        {
            Logger.LogException("MapConfig", ex);
        }
    }

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

        if (_mapConfigData.Spawns.Any(existingSpawn =>
                existingSpawn.Vector == spawn.Vector && existingSpawn.Bombsite == spawn.Bombsite))
        {
            Logger.LogWarning("MapConfig", "Spawn already exists, avoiding duplication");
            return false;
        }

        _mapConfigData.Spawns.Add(spawn);
        Save();
        Load();

        Logger.LogInfo("MapConfig", "Spawn added successfully");
        return true;
    }

    public bool RemoveSpawn(Spawn spawn)
    {
        _mapConfigData ??= new MapConfigData();

        if (!_mapConfigData.Spawns.Any(existingSpawn =>
                existingSpawn.Vector == spawn.Vector && existingSpawn.Bombsite == spawn.Bombsite))
        {
            Logger.LogWarning("MapConfig", "Spawn doesn't exist, nothing to remove");
            return false;
        }

        _mapConfigData.Spawns.Remove(spawn);
        Save();
        Load();

        Logger.LogInfo("MapConfig", "Spawn removed successfully");
        return true;
    }

    private MapConfigData GetSanitizedMapConfigData()
    {
        if (_mapConfigData == null)
        {
            throw new Exception("Map config data is null");
        }

        _mapConfigData.Spawns = _mapConfigData.Spawns
            .GroupBy(spawn => new { spawn.Vector, spawn.Bombsite })
            .Select(group => group.First())
            .ToList();

        return _mapConfigData;
    }

    private void Save()
    {
        var jsonString = JsonSerializer.Serialize(GetSanitizedMapConfigData(), _jsonOptions);

        try
        {
            if (!Directory.Exists(_mapConfigDirectory))
            {
                Directory.CreateDirectory(_mapConfigDirectory);
            }

            File.WriteAllText(_mapConfigPath, jsonString);
            Logger.LogDebug("MapConfig", $"Data written to {_mapConfigPath}");
        }
        catch (IOException e)
        {
            Logger.LogError("MapConfig", $"Error writing to file: {e.Message}");
        }
    }

    public static bool IsLoaded(MapConfigService? mapConfig, string currentMap)
    {
        if (mapConfig == null || mapConfig._mapName != currentMap)
        {
            return false;
        }

        return true;
    }
}