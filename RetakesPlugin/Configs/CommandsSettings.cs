using System.Text.Json.Serialization;

namespace RetakesPlugin.Configs;

public class CommandsSettings
{
    [JsonPropertyName("Admin")]
    public Dictionary<string, string> Admin { get; set; } = new()
    {
        { "css_forcebombsite", "@css/root" },
        { "css_forcebombsitestop", "@css/root" },
        { "css_scramble", "@css/admin" },
        { "css_scrambleteams", "@css/admin" },
        { "css_debugqueues", "@css/root" }
    };

    [JsonPropertyName("MapConfig")]
    public Dictionary<string, string> MapConfig { get; set; } = new()
    {
        { "css_mapconfig", "@css/root" },
        { "css_setmapconfig", "@css/root" },
        { "css_loadmapconfig", "@css/root" },
        { "css_mapconfigs", "@css/root" },
        { "css_viewmapconfigs", "@css/root" },
        { "css_listmapconfigs", "@css/root" }
    };

    [JsonPropertyName("SpawnEditor")]
    public Dictionary<string, string> SpawnEditor { get; set; } = new()
    {
        { "css_showspawns", "@css/root" },
        { "css_spawns", "@css/root" },
        { "css_edit", "@css/root" },
        { "css_addspawn", "@css/root" },
        { "css_add", "@css/root" },
        { "css_newspawn", "@css/root" },
        { "css_new", "@css/root" },
        { "css_removespawn", "@css/root" },
        { "css_remove", "@css/root" },
        { "css_deletespawn", "@css/root" },
        { "css_delete", "@css/root" },
        { "css_nearestspawn", "@css/root" },
        { "css_nearest", "@css/root" },
        { "css_hidespawns", "@css/root" },
        { "css_done", "@css/root" },
        { "css_exitedit", "@css/root" }
    };
}