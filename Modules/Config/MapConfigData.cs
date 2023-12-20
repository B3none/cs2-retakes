namespace RetakesPlugin.Modules.Config;

public class MapConfigData
{
    public List<Spawn> Spawns { get; set; }
    
    public MapConfigData(List<Spawn>? spawns = null)
    {
        Spawns = spawns ?? new List<Spawn>();
    }
}