using System.Text.Json.Serialization;
using CounterStrikeSharp.API.Modules.Utils;

using RetakesPlugin.Configs.JsonConverters;
using RetakesPluginShared.Enums;

namespace RetakesPlugin.Models;

public class Spawn
{
    public Spawn(Vector vector, QAngle qAngle)
    {
        Vector = vector;
        QAngle = qAngle;
    }

    [JsonConverter(typeof(VectorJsonConverter))]
    public Vector Vector { get; }

    [JsonConverter(typeof(QAngleJsonConverter))]
    public QAngle QAngle { get; }

    public CsTeam Team { get; set; }
    public Bombsite Bombsite { get; set; }
    public bool CanBePlanter { get; set; }
}