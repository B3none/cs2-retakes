using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;

namespace RetakesPlugin.Modules.Managers;

public class Game
{
    private Dictionary<int, int> _playerRoundScores = new();
    public readonly Queue Queue = new();

    public const int ScoreForKill = 50;
    public const int ScoreForAssist = 25;
    public const int ScoreForDefuse = 50;

    private void ScrambleTeams()
    {
        var numAssigned = 0;
        
        foreach (var player in Helpers.Shuffle(Queue.ActivePlayers))
        {
            player.SwitchTeam(numAssigned < Queue.GetNumTerrorists() ? CsTeam.Terrorist : CsTeam.CounterTerrorist);
            numAssigned++;
        }
    }

    public void ResetPlayerScores()
    {
        _playerRoundScores = new Dictionary<int, int>();
    }
    
    public void AddScore(CCSPlayerController player, int score)
    {
        if (!Helpers.IsValidPlayer(player) || player.UserId == null)
        {
            return;
        }
    
        var playerId = (int)player.UserId;

        if (!_playerRoundScores.TryAdd(playerId, score))
        {
            // Add to the player's existing score
            _playerRoundScores[playerId] += score;
        }
    }

    private int _consecutiveRoundsWon = 0;
    private const int ConsecutiveRoundWinsToScramble = 5;
    
    public void TerroristRoundWin()
    {
        _consecutiveRoundsWon++;
        
        if (_consecutiveRoundsWon == ConsecutiveRoundWinsToScramble)
        {
            _consecutiveRoundsWon = 0;
            ScrambleTeams();
        }
    }
    
    public void CounterTerroristRoundWin()
    {
        _consecutiveRoundsWon = 0;
        
        var numTerrorists = Queue.GetNumTerrorists();
        
        var sortedCounterTerroristPlayers = Queue.ActivePlayers
            .Where(player => player.TeamNum == (int)CsTeam.CounterTerrorist && Helpers.IsValidPlayer(player))
            .OrderByDescending(player => _playerRoundScores.GetValueOrDefault((int)player.UserId!, 0))
            .ToList();
        
        var newTerrorists = sortedCounterTerroristPlayers.Where(player => player.Score > 0).Take(numTerrorists).ToList();
        
        var playersLeft = Helpers.Shuffle(sortedCounterTerroristPlayers.Except(newTerrorists).ToList());
        newTerrorists.AddRange(playersLeft.Take(numTerrorists - newTerrorists.Count));

        foreach (var player in Utilities.GetPlayers().Where(Helpers.IsValidPlayer))
        {
            if (player.TeamNum == (int)CsTeam.Terrorist)
            {
                player.SwitchTeam(CsTeam.CounterTerrorist);
                continue;
            }
            
            if (newTerrorists.Contains(player))
            {
                player.SwitchTeam(CsTeam.Terrorist);
            }
        }
    }

    public void SetupActivePlayers()
    {
        if (Queue.ActivePlayers.Count != 0)
        {
            return;
        }
        
        Queue.ActivePlayers = Queue.QueuePlayers;
        Queue.QueuePlayers.Clear();
    }
}