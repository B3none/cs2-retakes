using CounterStrikeSharp.API.Core;

namespace RetakesPlugin.Modules.Managers;

public class Queue
{
    public static int MaxRetakesPlayers = 9;
    public static float TerroristRatio = 0.45f;
    
    public List<CCSPlayerController> QueuePlayers = new();
    public List<CCSPlayerController> ActivePlayers = new();

    public int GetNumTerrorists()
    {
        var ratio = TerroristRatio * ActivePlayers.Count;
        var numTerrorists = (int)Math.Round(ratio);

        // Ensure at least one terrorist if the calculated number is zero
        return numTerrorists > 0 ? numTerrorists : 1;
    }
    
    public int GetNumCounterTerrorists()
    {
        int numTerrorists = GetNumTerrorists();
        return ActivePlayers.Count - numTerrorists;
    }

    public void UpdateActivePlayers()
    {
        if (ActivePlayers.Count < MaxRetakesPlayers && QueuePlayers.Count > 0)
        {
            foreach (var queuePlayer in QueuePlayers)
            {
                // get queuePlayer index in QueuePlayers
            }
        }
    }
}