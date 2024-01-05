namespace RetakesPlugin.Modules.Configs;

public class RetakesConfigData
{
    public static int CurrentVersion = 2;

    public int Version { get; set; } = CurrentVersion;
    public int MaxPlayers { get; set; } = 9;
    public float TerroristRatio { get; set; } = 0.45f;
    public int RoundsToScramble { get; set; } = 5;
    public bool EnableFallbackAllocation { get; set; } = true;
    public bool EnableBombsiteAnnouncementVoices { get; set; } = true;
    public bool EnableBombsiteAnnouncementCenter { get; set; } = true;
}
