using System.Text.Json.Serialization;

namespace RetakesPlugin.Configs;

public class TeamSettings
{
    [JsonPropertyName("TerroristRatio")]
    public float TerroristRatio { get; set; } = 0.45f;

    [JsonPropertyName("RoundsToScramble")]
    public int RoundsToScramble { get; set; } = 5;

    [JsonPropertyName("IsScrambleEnabled")]
    public bool IsScrambleEnabled { get; set; } = true;

    [JsonPropertyName("IsBalanceEnabled")]
    public bool IsBalanceEnabled { get; set; } = true;

    [JsonPropertyName("ShouldForceEvenTeamsWhenPlayerCountIsMultipleOf10")]
    public bool ShouldForceEvenTeamsWhenPlayerCountIsMultipleOf10 { get; set; } = true;

    [JsonPropertyName("ShouldPreventTeamChangesMidRound")]
    public bool ShouldPreventTeamChangesMidRound { get; set; } = true;
}