namespace RetakesPlugin.Modules.Configs;

public class RetakesConfigData
{
    public static int CurrentVersion = 8;

    public int Version { get; set; } = CurrentVersion;
    public int MaxPlayers { get; set; } = 9;
    public float TerroristRatio { get; set; } = 0.45f;
    public int RoundsToScramble { get; set; } = 5;
    public bool IsScrambleEnabled { get; set; } = true;
    public bool EnableFallbackAllocation { get; set; } = true;
    public bool EnableBombsiteAnnouncementVoices { get; set; } = true;
    public bool EnableBombsiteAnnouncementCenter { get; set; } = true;
    public bool ShouldBreakBreakables { get; set; } = false;
    public bool ShouldOpenDoors { get; set; } = false;
    public bool IsAutoPlantEnabled { get; set; } = true;
    public string QueuePriorityFlag { get; set; } = "@css/vip";
    public bool IsDebugMode { get; set; } = false;
    public bool ShouldForceEvenTeamsWhenPlayerCountIsMultipleOf10 { get; set; } = true;
    public bool EnableFallbackBombsiteAnnouncement { get; set; } = true;
}
