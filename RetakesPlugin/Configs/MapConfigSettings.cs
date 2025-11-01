using System.Text.Json.Serialization;

namespace RetakesPlugin.Configs;

public class MapConfigSettings
{
    [JsonPropertyName("EnableBombsiteAnnouncementVoices")]
    public bool EnableBombsiteAnnouncementVoices { get; set; } = false;

    [JsonPropertyName("EnableBombsiteAnnouncementCenter")]
    public bool EnableBombsiteAnnouncementCenter { get; set; } = true;

    [JsonPropertyName("EnableFallbackBombsiteAnnouncement")]
    public bool EnableFallbackBombsiteAnnouncement { get; set; } = true;
}