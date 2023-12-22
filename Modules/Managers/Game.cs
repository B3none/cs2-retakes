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
            player.SwitchTeam(numAssigned < Queue.GetTargetNumTerrorists() ? CsTeam.Terrorist : CsTeam.CounterTerrorist);
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
            return;
        }

        var numTerroristsNeeded = Queue.GetTargetNumTerrorists() - Helpers.GetCurrentNumTerrorists();
        Console.WriteLine($"{RetakesPlugin.MessagePrefix}Terrorists won, checking if they need a player. Queue.GetTargetNumTerrorists() = {Queue.GetTargetNumTerrorists()} | Helpers.GetCurrentNumTerrorists() = {Helpers.GetCurrentNumTerrorists()} | numTerroristsNeeded {numTerroristsNeeded}");

        if (!(numTerroristsNeeded > 0))
        {
            Console.WriteLine($"{RetakesPlugin.MessagePrefix}No terrorists needed");
            return;
        }
        
        Console.WriteLine($"{RetakesPlugin.MessagePrefix}{numTerroristsNeeded} terrorists needed");
        
        var sortedCounterTerroristPlayers = Queue.ActivePlayers
            .Where(player => Helpers.IsValidPlayer(player) && player.TeamNum == (int)CsTeam.CounterTerrorist)
            .OrderByDescending(player => _playerRoundScores.GetValueOrDefault((int)player.UserId!, 0))
            .ToList();
        
        Console.WriteLine($"{RetakesPlugin.MessagePrefix}Got {sortedCounterTerroristPlayers.Count} sortedCounterTerroristPlayers.");
        
        var newTerrorists = sortedCounterTerroristPlayers.Where(player => player.Score > 0).Take(numTerroristsNeeded).ToList();
        
        Console.WriteLine($"{RetakesPlugin.MessagePrefix}{sortedCounterTerroristPlayers.Where(player => player.Score > 0).ToList().Count} sortedCounterTerroristPlayers with more than 0 score found.");
        Console.WriteLine($"{RetakesPlugin.MessagePrefix}There are currently {newTerrorists.Count} new terrorists.");

        if (newTerrorists.Count < numTerroristsNeeded)
        {
            Console.WriteLine($"{RetakesPlugin.MessagePrefix}Still not enough terrorists needed.");
            var playersLeft = Helpers.Shuffle(sortedCounterTerroristPlayers.Except(newTerrorists).ToList());
            newTerrorists.AddRange(playersLeft.Take(numTerroristsNeeded - newTerrorists.Count));
        }

        if (newTerrorists.Count < numTerroristsNeeded)
        {
            Console.WriteLine($"{RetakesPlugin.MessagePrefix}Still not enough terrorists needed.");
            var playersLeft = Helpers.Shuffle(sortedCounterTerroristPlayers.Except(newTerrorists).ToList());
            newTerrorists.AddRange(playersLeft.Take(numTerroristsNeeded - newTerrorists.Count));
        }
        
        var sortedTerroristPlayers = Queue.ActivePlayers
            .Where(player => Helpers.IsValidPlayer(player) && player.TeamNum == (int)CsTeam.Terrorist)
            .OrderByDescending(player => _playerRoundScores.GetValueOrDefault((int)player.UserId!, 0))
            .ToList();
        
        Console.WriteLine($"{RetakesPlugin.MessagePrefix}Got {sortedTerroristPlayers.Count} sortedTerroristPlayers.");
        
        newTerrorists.AddRange(sortedCounterTerroristPlayers.Where(player => player.Score > 0).Take(numTerroristsNeeded - newTerrorists.Count).ToList());
        
        Console.WriteLine($"{RetakesPlugin.MessagePrefix}{sortedTerroristPlayers.Where(player => player.Score > 0).ToList().Count} sortedTerroristPlayers with more than 0 score found.");
        Console.WriteLine($"{RetakesPlugin.MessagePrefix}There are currently {newTerrorists.Count} new terrorists.");

        if (newTerrorists.Count < numTerroristsNeeded)
        {
            Console.WriteLine($"{RetakesPlugin.MessagePrefix}Still not enough terrorists needed.");
            var playersLeft = Helpers.Shuffle(sortedCounterTerroristPlayers.Except(newTerrorists).ToList());
            newTerrorists.AddRange(playersLeft.Take(numTerroristsNeeded - newTerrorists.Count));
        }

        Console.WriteLine($"{RetakesPlugin.MessagePrefix}Swapping players to terrorist.");
        foreach (var player in newTerrorists)
        {
            Console.WriteLine($"{RetakesPlugin.MessagePrefix}Swapping player {player.PlayerName} to terrorist.");
            player.SwitchTeam(CsTeam.Terrorist);
        }
    }
    
    public void CounterTerroristRoundWin()
    {
        _consecutiveRoundsWon = 0;
        
        var targetNumTerrorists = Queue.GetTargetNumTerrorists();
        
        var sortedCounterTerroristPlayers = Queue.ActivePlayers
            .Where(player => player.TeamNum == (int)CsTeam.CounterTerrorist && Helpers.IsValidPlayer(player))
            .OrderByDescending(player => _playerRoundScores.GetValueOrDefault((int)player.UserId!, 0))
            .ToList();
        
        var newTerrorists = sortedCounterTerroristPlayers.Where(player => player.Score > 0).Take(targetNumTerrorists).ToList();

        if (newTerrorists.Count < targetNumTerrorists)
        {
            var playersLeft = Helpers.Shuffle(sortedCounterTerroristPlayers.Except(newTerrorists).ToList());
            newTerrorists.AddRange(playersLeft.Take(targetNumTerrorists - newTerrorists.Count));
        }
        
        if (newTerrorists.Count < targetNumTerrorists)
        {
            Console.WriteLine($"{RetakesPlugin.MessagePrefix}Still not enough terrorists needed.");
            var playersLeft = Helpers.Shuffle(sortedCounterTerroristPlayers.Except(newTerrorists).ToList());
            newTerrorists.AddRange(playersLeft.Take(targetNumTerrorists - newTerrorists.Count));
        }
        
        var sortedTerroristPlayers = Queue.ActivePlayers
            .Where(player => Helpers.IsValidPlayer(player) && player.TeamNum == (int)CsTeam.Terrorist)
            .OrderByDescending(player => _playerRoundScores.GetValueOrDefault((int)player.UserId!, 0))
            .ToList();
        
        Console.WriteLine($"{RetakesPlugin.MessagePrefix}Got {sortedTerroristPlayers.Count} sortedTerroristPlayers.");
        
        newTerrorists.AddRange(sortedCounterTerroristPlayers.Where(player => player.Score > 0).Take(targetNumTerrorists - newTerrorists.Count).ToList());
        
        Console.WriteLine($"{RetakesPlugin.MessagePrefix}{sortedTerroristPlayers.Where(player => player.Score > 0).ToList().Count} sortedTerroristPlayers with more than 0 score found.");
        Console.WriteLine($"{RetakesPlugin.MessagePrefix}There are currently {newTerrorists.Count} new terrorists.");

        if (newTerrorists.Count < targetNumTerrorists)
        {
            Console.WriteLine($"{RetakesPlugin.MessagePrefix}Still not enough terrorists needed.");
            var playersLeft = Helpers.Shuffle(sortedCounterTerroristPlayers.Except(newTerrorists).ToList());
            newTerrorists.AddRange(playersLeft.Take(targetNumTerrorists - newTerrorists.Count));
        }

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
        Queue.QueuePlayers = new List<CCSPlayerController>();
    }
}