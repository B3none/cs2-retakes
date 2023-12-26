namespace RetakesPlugin.Modules.Config;

public class RetakesConfigData
{
    public static int CurrentVersion = 1;

    public int Version { get; set; } = 1;
    public int MaxPlayers { get; set; } = 9;
    public float TerroristRatio { get; set; } = 0.45f;
    public int RoundsToScramble { get; set; } = 5;
    public bool EnableAllocation { get; set; } = true;
}
