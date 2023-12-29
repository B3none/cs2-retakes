using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;

namespace RetakesPlugin.Modules.Managers;

public class Game
{
    private Dictionary<int, int> _playerRoundScores = new();
    public readonly Queue Queue;
    private readonly int _consecutiveRoundWinsToScramble;

    public const int ScoreForKill = 50;
    public const int ScoreForAssist = 25;
    public const int ScoreForDefuse = 50;

    public Game(Queue queue, int? roundsToScramble)
    {
        Queue = queue;
        _consecutiveRoundWinsToScramble = roundsToScramble ?? 5;
    }
    
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
    
    public void TerroristRoundWin()
    {
        _consecutiveRoundsWon++;
        
        if (_consecutiveRoundsWon == _consecutiveRoundWinsToScramble)
        {
            _consecutiveRoundsWon = 0;
            ScrambleTeams();
            return;
        }

        BalanceTeams();
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
            Console.WriteLine($"{RetakesPlugin.LogPrefix}Still not enough terrorists needed.");
            var playersLeft = Helpers.Shuffle(sortedCounterTerroristPlayers.Except(newTerrorists).ToList());
            newTerrorists.AddRange(playersLeft.Take(targetNumTerrorists - newTerrorists.Count));
        }
        
        var sortedTerroristPlayers = Queue.ActivePlayers
            .Where(player => Helpers.IsValidPlayer(player) && player.TeamNum == (int)CsTeam.Terrorist)
            .OrderByDescending(player => _playerRoundScores.GetValueOrDefault((int)player.UserId!, 0))
            .ToList();
        
        Console.WriteLine($"{RetakesPlugin.LogPrefix}Got {sortedTerroristPlayers.Count} sortedTerroristPlayers.");
        
        newTerrorists.AddRange(sortedCounterTerroristPlayers.Where(player => player.Score > 0).Take(targetNumTerrorists - newTerrorists.Count).ToList());
        
        Console.WriteLine($"{RetakesPlugin.LogPrefix}{sortedTerroristPlayers.Where(player => player.Score > 0).ToList().Count} sortedTerroristPlayers with more than 0 score found.");
        Console.WriteLine($"{RetakesPlugin.LogPrefix}There are currently {newTerrorists.Count} new terrorists.");

        if (newTerrorists.Count < targetNumTerrorists)
        {
            Console.WriteLine($"{RetakesPlugin.LogPrefix}Still not enough terrorists needed.");
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

    public void BalanceTeams()
    {
        Console.WriteLine($"{RetakesPlugin.LogPrefix}Balancing teams...");
        
        var currentNumTerrorist = Helpers.GetCurrentNumPlayers(CsTeam.Terrorist);
        
        var numTerroristsNeeded = Queue.GetTargetNumTerrorists() - currentNumTerrorist;
        Console.WriteLine($"{RetakesPlugin.LogPrefix}Checking if they terrorists need a player. Queue.GetTargetNumTerrorists() = {Queue.GetTargetNumTerrorists()} | Helpers.GetCurrentNumPlayers(CsTeam.Terrorist) = {currentNumTerrorist} | numTerroristsNeeded {numTerroristsNeeded}");
        
        List<CCSPlayerController> newTerrorists = new();
        List<CCSPlayerController> newCounterTerrorists = new();
        
        if (numTerroristsNeeded > 0)
        {
            Console.WriteLine($"{RetakesPlugin.LogPrefix}{numTerroristsNeeded} terrorists needed");

            var sortedCounterTerroristPlayers = /*Queue.ActivePlayers*/Utilities.GetPlayers()
                .Where(player => Helpers.IsValidPlayer(player) && player.TeamNum == (int)CsTeam.CounterTerrorist)
                .OrderByDescending(player => _playerRoundScores.GetValueOrDefault((int)player.UserId!, 0))
                .ToList();

            Console.WriteLine(
                $"{RetakesPlugin.LogPrefix}Got {sortedCounterTerroristPlayers.Count} sortedCounterTerroristPlayers.");

            newTerrorists = sortedCounterTerroristPlayers.Where(player => player.Score > 0)
                .Take(numTerroristsNeeded).ToList();

            Console.WriteLine(
                $"{RetakesPlugin.LogPrefix}{sortedCounterTerroristPlayers.Where(player => player.Score > 0).ToList().Count} sortedCounterTerroristPlayers with more than 0 score found.");
            Console.WriteLine(
                $"{RetakesPlugin.LogPrefix}There are currently {newTerrorists.Count} new terrorists.");

            if (newTerrorists.Count < numTerroristsNeeded)
            {
                Console.WriteLine($"{RetakesPlugin.LogPrefix}Still not enough terrorists needed.");
                var playersLeft = Helpers.Shuffle(sortedCounterTerroristPlayers.Except(newTerrorists).ToList());
                newTerrorists.AddRange(playersLeft.Take(numTerroristsNeeded - newTerrorists.Count));
            }

            Console.WriteLine($"{RetakesPlugin.LogPrefix}Swapping player(s) to terrorist...");
            foreach (var player in newTerrorists)
            {
                Console.WriteLine($"{RetakesPlugin.LogPrefix}Swapping player {player.PlayerName} to terrorist.");
                player.SwitchTeam(CsTeam.Terrorist);
            }
        }
        
        var currentNumTerroristAfterBalance = Helpers.GetCurrentNumPlayers(CsTeam.Terrorist);
        var currentNumCounterTerroristAfterBalance = Helpers.GetCurrentNumPlayers(CsTeam.CounterTerrorist);
        var numCounterTerroristsNeeded = Queue.GetTargetNumCounterTerrorists() - currentNumCounterTerroristAfterBalance;
        Console.WriteLine($"{RetakesPlugin.LogPrefix}checking if CT need a player. Queue.GetTargetNumCounterTerrorists() = {Queue.GetTargetNumCounterTerrorists()} | Helpers.GetCurrentNumPlayers(CsTeam.CounterTerrorist) = {currentNumTerroristAfterBalance} | numCounterTerroristsNeeded {numCounterTerroristsNeeded}");
        
        if (currentNumTerroristAfterBalance > 1 && numCounterTerroristsNeeded > 0)
        {
            var terroristsWithZeroScore = /*Queue.ActivePlayers*/Utilities.GetPlayers()
                .Where(player => 
                    player.TeamNum == (int)CsTeam.Terrorist
                    && Helpers.IsValidPlayer(player) 
                    && _playerRoundScores.GetValueOrDefault((int)player.UserId!, 0) == 0
                )
                .Except(newTerrorists)
                .ToList();
            Console.WriteLine($"{RetakesPlugin.LogPrefix}Found {terroristsWithZeroScore} terrorists with 0 score.");
            
            Console.WriteLine($"{RetakesPlugin.LogPrefix}Moving terrorists with 0 score to CT.");

            // Shuffle to avoid repetitive swapping of the same players
            newCounterTerrorists = Helpers.Shuffle(terroristsWithZeroScore).Take(numCounterTerroristsNeeded).ToList();

            if (numCounterTerroristsNeeded > newCounterTerrorists.Count)
            {
                // For remaining excess terrorists, move the ones with the lowest score to CT
                newCounterTerrorists.AddRange(
                    /*Queue.ActivePlayers*/Utilities.GetPlayers()
                        .Except(newCounterTerrorists)
                        .Except(newTerrorists)
                        .Where(player => player.TeamNum == (int)CsTeam.Terrorist && Helpers.IsValidPlayer(player))
                        .OrderBy(player => _playerRoundScores.GetValueOrDefault((int)player.UserId!, 0))
                        .Take(numTerroristsNeeded - newCounterTerrorists.Count)
                        .ToList()
                );
            }
            
            Console.WriteLine($"{RetakesPlugin.LogPrefix}Moving terrorists to CT (found {newCounterTerrorists.Count}).");
            foreach (var player in newCounterTerrorists)
            {
                Console.WriteLine($"{RetakesPlugin.LogPrefix}Swapping player {player.PlayerName} to CT.");
                player.SwitchTeam(CsTeam.CounterTerrorist);
            }
        }
        else
        {
            Console.WriteLine($"{RetakesPlugin.LogPrefix}No changes needed");
        }
        
        Console.WriteLine($"{RetakesPlugin.LogPrefix}Balance teams DONE");
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