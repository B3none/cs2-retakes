using System.Text.Json.Serialization;

namespace RetakesPlugin.Configs;

public class QueueSettings
{

    public string QueuePriorityFlag { get; set; } = "@css/vip";

    [JsonPropertyName("QueueImmunityFlag")]
    public string QueueImmunityFlag { get; set; } = "@css/vip";

    [JsonPropertyName("ShouldRemoveSpectators")]
    public bool ShouldRemoveSpectators { get; set; } = true;
}