using CounterStrikeSharp.API.Modules.Utils;

namespace RetakesPlugin.Modules.Config;

public abstract class Spawn
{
    public Vector Vector { get; set; }
    public QAngle Angle { get; set; }
    public double Z { get; set; }
    public CsTeam Team { get; set; }
    public Bombsite Bombsite { get; set; }
    public bool CanBePlanter { get; set; }
}
