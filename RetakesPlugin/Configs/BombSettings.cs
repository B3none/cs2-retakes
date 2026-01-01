using System.Text.Json.Serialization;

namespace RetakesPlugin.Configs;

public class BombSettings
{
    [JsonPropertyName("IsAutoPlantEnabled")]
    public bool IsAutoPlantEnabled { get; set; } = true;
}