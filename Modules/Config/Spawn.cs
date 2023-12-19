using CounterStrikeSharp.API.Modules.Utils;

namespace RetakesPlugin.Modules.Config;

public class Spawn
{
    public Spawn(Vector vector, QAngle angle)
    {
        Vector = vector;
        Angle = angle;
    }

    public Vector Vector { get; set; }
    public QAngle Angle { get; set; }
    public CsTeam Team { get; set; }
    public Bombsite Bombsite { get; set; }
    public bool CanBePlanter { get; set; }
}
