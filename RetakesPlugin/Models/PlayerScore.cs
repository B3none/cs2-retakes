namespace RetakesPlugin.Models;

public class PlayerScore
{
    public int UserId { get; set; }
    public string PlayerName { get; set; } = string.Empty;
    public int Score { get; set; }
    public int Kills { get; set; }
    public int Assists { get; set; }
    public int Defuses { get; set; }

    public void AddKill()
    {
        Kills++;
        Score += 50;
    }

    public void AddAssist()
    {
        Assists++;
        Score += 25;
    }

    public void AddDefuse()
    {
        Defuses++;
        Score += 50;
    }

    public void AddScore(int points)
    {
        Score += points;
    }

    public void Reset()
    {
        Score = 0;
        Kills = 0;
        Assists = 0;
        Defuses = 0;
    }
}
