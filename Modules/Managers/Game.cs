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
        var shuffledActivePlayers = Helpers.Shuffle(Queue.ActivePlayers);
        
        var newTerrorists = shuffledActivePlayers.Take(Queue.GetTargetNumTerrorists()).ToList();
        var newCounterTerrorists = shuffledActivePlayers.Except(newTerrorists).ToList();
        
        SetTeams(newTerrorists, newCounterTerrorists);
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
    
    private int _consecutiveRoundsWon;
    
    public void TerroristRoundWin()
    {
        _consecutiveRoundsWon++;
        
        if (_consecutiveRoundsWon == _consecutiveRoundWinsToScramble)
        {
            // TODO: Translate this message.
            Server.PrintToChatAll($"{RetakesPlugin.MessagePrefix}Terrorists have won {_consecutiveRoundWinsToScramble} rounds in a row! Teams will be scrambled.");
         
            _consecutiveRoundsWon = 0;
            ScrambleTeams();
        }
    }
    
    public void CounterTerroristRoundWin()
    {
        _consecutiveRoundsWon = 0;
        
        var targetNumTerrorists = Queue.GetTargetNumTerrorists();
        var sortedCounterTerroristPlayers = GetSortedActivePlayers(CsTeam.CounterTerrorist);
        
        // Ensure that the players with the scores are set as new terrorists first.
        var newTerrorists = sortedCounterTerroristPlayers.Where(player => player.Score > 0).Take(targetNumTerrorists).ToList();

        if (newTerrorists.Count < targetNumTerrorists)
        {
            // Shuffle the other players with 0 score to ensure it's random who is swapped
            var playersLeft = Helpers.Shuffle(sortedCounterTerroristPlayers.Except(newTerrorists).ToList());
            newTerrorists.AddRange(playersLeft.Take(targetNumTerrorists - newTerrorists.Count));
        }
        
        if (newTerrorists.Count < targetNumTerrorists)
        {
            // If we still don't have enough terrorists
            newTerrorists.AddRange(
                GetSortedActivePlayers(CsTeam.Terrorist)
                    .Take(targetNumTerrorists - newTerrorists.Count)
                );
        }
        
        newTerrorists.AddRange(sortedCounterTerroristPlayers.Where(player => player.Score > 0).Take(targetNumTerrorists - newTerrorists.Count).ToList());

        var newCounterTerrorists = Queue.ActivePlayers.Except(newTerrorists).ToList();
        
        SetTeams(newTerrorists, newCounterTerrorists);
    }

    public void BalanceTeams()
    {
        List<CCSPlayerController> newTerrorists = new();
        List<CCSPlayerController> newCounterTerrorists = new();
     
        var currentNumTerrorist = Helpers.GetCurrentNumPlayers(CsTeam.Terrorist);
        var numTerroristsNeeded = Queue.GetTargetNumTerrorists() - currentNumTerrorist;
        
        if (numTerroristsNeeded > 0)
        {
            var sortedCounterTerroristPlayers = GetSortedActivePlayers(CsTeam.CounterTerrorist);

            newTerrorists = sortedCounterTerroristPlayers.Where(player => player.Score > 0)
                .Take(numTerroristsNeeded).ToList();

            if (newTerrorists.Count < numTerroristsNeeded)
            {
                var playersLeft = Helpers.Shuffle(sortedCounterTerroristPlayers.Except(newTerrorists).ToList());
                newTerrorists.AddRange(playersLeft.Take(numTerroristsNeeded - newTerrorists.Count));
            }

            if (newTerrorists.Count < numTerroristsNeeded)
            {
                var sortedTerrorists = GetSortedActivePlayers(CsTeam.Terrorist);
                var playersLeft = Helpers.Shuffle(sortedTerrorists.Except(newTerrorists).ToList());
                newTerrorists.AddRange(playersLeft.Take(numTerroristsNeeded - newTerrorists.Count));
            }
        }
        
        var currentNumCounterTerroristAfterBalance = Helpers.GetCurrentNumPlayers(CsTeam.CounterTerrorist);
        var numCounterTerroristsNeeded = Queue.GetTargetNumCounterTerrorists() - currentNumCounterTerroristAfterBalance;
        
        if (numCounterTerroristsNeeded > 0)
        {
            var terroristsWithZeroScore = Queue.ActivePlayers
                .Where(player => 
                    (CsTeam)player.TeamNum == CsTeam.Terrorist
                    && Helpers.IsValidPlayer(player) 
                    && _playerRoundScores.GetValueOrDefault((int)player.UserId!, 0) == 0
                )
                .Except(newTerrorists)
                .ToList();

            // Shuffle to avoid repetitive swapping of the same players
            newCounterTerrorists = Helpers.Shuffle(terroristsWithZeroScore).Take(numCounterTerroristsNeeded).ToList();

            if (numCounterTerroristsNeeded > newCounterTerrorists.Count)
            {
                // For remaining excess terrorists, move the ones with the lowest score to CT
                newCounterTerrorists.AddRange(
                    Queue.ActivePlayers
                        .Except(newCounterTerrorists)
                        .Except(newTerrorists)
                        .Where(player => (CsTeam)player.TeamNum == CsTeam.Terrorist && Helpers.IsValidPlayer(player))
                        .OrderBy(player => _playerRoundScores.GetValueOrDefault((int)player.UserId!, 0))
                        .Take(numTerroristsNeeded - newCounterTerrorists.Count)
                        .ToList()
                );
            }
        }
        
        SetTeams(newTerrorists, newCounterTerrorists);
    }

    private List<CCSPlayerController> GetSortedActivePlayers(CsTeam? team = null)
    {
        return Queue.ActivePlayers
            .Where(Helpers.IsValidPlayer)
            .Where(player => team == null || (CsTeam)player.TeamNum == team)
            .OrderByDescending(player => _playerRoundScores.GetValueOrDefault((int)player.UserId!, 0))
            .ToList();
    }

    private void SetTeams(List<CCSPlayerController>? terrorists, List<CCSPlayerController>? counterTerrorists)
    {
        terrorists ??= new List<CCSPlayerController>();
        counterTerrorists ??= new List<CCSPlayerController>();
        
        foreach (var player in Queue.ActivePlayers)
        {
            if (terrorists.Contains(player))
            {
                if ((CsTeam)player.TeamNum != CsTeam.Terrorist)
                {
                    player.SwitchTeam(CsTeam.Terrorist);
                }
            }
            else if (counterTerrorists.Contains(player))
            {
                if ((CsTeam)player.TeamNum != CsTeam.CounterTerrorist)
                {
                    player.SwitchTeam(CsTeam.CounterTerrorist);
                }
            }
        }
    }
}