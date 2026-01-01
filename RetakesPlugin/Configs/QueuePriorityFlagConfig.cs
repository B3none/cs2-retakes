using System.Text.Json.Serialization;

namespace RetakesPlugin.Configs;

public class QueuePriorityFlagConfig
{
    [JsonPropertyName("DisplayName")]
    public string DisplayName { get; set; } = string.Empty;

    [JsonPropertyName("Flag")]
    public string Flag { get; set; } = string.Empty;

    private int _priority = 0;

    [JsonPropertyName("Priority")]
    public int Priority
    {
        get => _priority;
        set => _priority = Math.Clamp(value, 0, 100);
    }
}