using CounterStrikeSharp.API.Modules.Utils;

namespace RetakesPlugin.Modules.Managers;

public class Teams
{
    private Dictionary<int, int> _playerRoundScores = new();
    public readonly Queue Queue = new();
    
    public void Scramble()
    {
        var numAssigned = 0;
        foreach (var player in Helpers.Shuffle(Queue.ActivePlayers))
        {
            player.ChangeTeam(numAssigned < Queue.NumTerrorists ? CsTeam.Terrorist : CsTeam.CounterTerrorist);
        }
    }
}