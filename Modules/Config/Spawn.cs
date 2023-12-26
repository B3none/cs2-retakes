using System.Text.Json.Serialization;
using CounterStrikeSharp.API.Modules.Utils;
using RetakesPlugin.Modules.Converters;

namespace RetakesPlugin.Modules.Config;

public class Spawn
{
    public Spawn(Vector vector, QAngle qAngle)
    {
        Vector = vector;
        QAngle = qAngle;
    }

    [JsonConverter(typeof(VectorConverter))]
    public Vector Vector { get; }
    
    [JsonConverter(typeof(QAngleConverter))]
    public QAngle QAngle { get; }
    public CsTeam Team { get; set; }
    public Bombsite Bombsite { get; set; }
    public bool CanBePlanter { get; set; }
}
