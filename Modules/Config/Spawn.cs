using CounterStrikeSharp.API.Modules.Utils;

namespace RetakesPlugin.Modules.Config;

public abstract class Spawn
{
    public double X { get; set; }
    public double Y { get; set; }
    public double Z { get; set; }
    public CsTeam Team { get; set; }
    public bool CanBePlanter { get; set; }
}
