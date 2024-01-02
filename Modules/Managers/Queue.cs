using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;

namespace RetakesPlugin.Modules.Managers;

public class Queue
{
    private readonly int _maxRetakesPlayers;
    private readonly float _terroristRatio;

    public List<CCSPlayerController> QueuePlayers = new();
    public List<CCSPlayerController> ActivePlayers = new();

    public List<CCSPlayerController> RoundTerrorists = new();
    public List<CCSPlayerController> RoundCounterTerrorists = new();

    public Queue(int? retakesMaxPlayers, float? retakesTerroristRatio)
    {
        _maxRetakesPlayers = retakesMaxPlayers ?? 9;
        _terroristRatio = retakesTerroristRatio ?? 0.45f;
    }

    public int GetTargetNumTerrorists()
    {
        var ratio = _terroristRatio * ActivePlayers.Count;
        var numTerrorists = (int)Math.Round(ratio);

        // Ensure at least one terrorist if the calculated number is zero
        return numTerrorists > 0 ? numTerrorists : 1;
    }
    
    public int GetTargetNumCounterTerrorists()
    {
        return ActivePlayers.Count - GetTargetNumTerrorists();
    }

    public void PlayerTriedToJoinTeam(CCSPlayerController player, CsTeam fromTeam, CsTeam toTeam)
    {
        Console.WriteLine($"{RetakesPlugin.LogPrefix}[{player.PlayerName}] PlayerTriedToJoinTeam called.");
        
        if (fromTeam == CsTeam.None && toTeam == CsTeam.Spectator)
        {
            // This is called when a player first joins.
            Console.WriteLine($"{RetakesPlugin.LogPrefix}[{player.PlayerName}] None -> Spectator.");
            return;
        }
        
        Console.WriteLine($"{RetakesPlugin.LogPrefix}[{player.PlayerName}] Checking ActivePlayers.");
        if (ActivePlayers.Contains(player))
        {
            Console.WriteLine($"{RetakesPlugin.LogPrefix}[{player.PlayerName}] Player is an active player.");
            
            if (toTeam == CsTeam.Spectator)
            {
                Console.WriteLine($"{RetakesPlugin.LogPrefix}[{player.PlayerName}] Switching to spectator.");
                ActivePlayers.Remove(player);
                return;
            }
            
            if (
                RoundTerrorists.Count > 0 
                && RoundCounterTerrorists.Count > 0
                && (
                    (toTeam == CsTeam.CounterTerrorist && !RoundCounterTerrorists.Contains(player))
                    || (toTeam == CsTeam.Terrorist && !RoundTerrorists.Contains(player))
                )
            )
            {
                Console.WriteLine($"{RetakesPlugin.LogPrefix}[{player.PlayerName}] player is not in round list for {toTeam}, switching to spectator.");
                ActivePlayers.Remove(player);
                QueuePlayers.Add(player);
                player.CommitSuicide(false, true);
                player.ChangeTeam(CsTeam.Spectator);
                return;
            }
            
            Console.WriteLine($"{RetakesPlugin.LogPrefix}[{player.PlayerName}] Do nothing.");
            return;
        }

        Console.WriteLine($"{RetakesPlugin.LogPrefix}[{player.PlayerName}] Checking QueuePlayers.");
        if (!QueuePlayers.Contains(player))
        {
            if (RetakesPlugin.GetGameRules().WarmupPeriod && ActivePlayers.Count < _maxRetakesPlayers)
            {
                Console.WriteLine($"{RetakesPlugin.LogPrefix}[{player.PlayerName}] Not found, adding to ActivePlayers (because in warmup).");
                ActivePlayers.Add(player);
                return;
            }
            
            Console.WriteLine($"{RetakesPlugin.LogPrefix}[{player.PlayerName}] Not found, adding to QueuePlayers.");
            QueuePlayers.Add(player);
        }
        else
        {
            Console.WriteLine($"{RetakesPlugin.LogPrefix}[{player.PlayerName}] Found in QueuePlayers, do nothing.");
        }

        Console.WriteLine($"{RetakesPlugin.LogPrefix}[{player.PlayerName}] Should switch to spectator? {(toTeam != CsTeam.Spectator ? "yes" : "no")}");
        if (toTeam != CsTeam.Spectator)
        {
            Console.WriteLine($"{RetakesPlugin.LogPrefix}[{player.PlayerName}] Changing to spectator.");
            player.CommitSuicide(false, true);
            player.ChangeTeam(CsTeam.Spectator);
        }
    }

    private void RemoveDisconnectedPlayers()
    {
        var disconnectedActivePlayers = ActivePlayers.Where(player => !Helpers.IsValidPlayer(player) || !Helpers.IsPlayerConnected(player)).ToList();

        if (disconnectedActivePlayers.Count > 0)
        {
            Console.WriteLine($"{RetakesPlugin.LogPrefix}Removing {disconnectedActivePlayers.Count} disconnected players from ActivePlayers.");
            ActivePlayers.RemoveAll(player => disconnectedActivePlayers.Contains(player));
        }
        
        var disconnectedQueuePlayers = QueuePlayers.Where(player => !Helpers.IsValidPlayer(player) || !Helpers.IsPlayerConnected(player)).ToList();
        
        if (disconnectedQueuePlayers.Count > 0)
        {
            Console.WriteLine($"{RetakesPlugin.LogPrefix}Removing {disconnectedQueuePlayers.Count} disconnected players from QueuePlayers.");
            QueuePlayers.RemoveAll(player => disconnectedQueuePlayers.Contains(player));
        }
    }
    
    public void Update()
    {
        RemoveDisconnectedPlayers();
        
        var playersToAdd = _maxRetakesPlayers - ActivePlayers.Count;

        if (playersToAdd > 0 && QueuePlayers.Count > 0)
        {
            // Take players from QueuePlayers and add them to ActivePlayers
            var playersToAddList = QueuePlayers.Take(playersToAdd).ToList();
            
            QueuePlayers.RemoveAll(player => playersToAddList.Contains(player));
            ActivePlayers.AddRange(playersToAddList);
            
            // loop players to add, and set their team to CT
            foreach (var player in playersToAddList)
            {
                player.SwitchTeam(CsTeam.CounterTerrorist);
            }
        }
    }

    public void PlayerDisconnected(CCSPlayerController player)
    {
        ActivePlayers.Remove(player);
        QueuePlayers.Remove(player);
    }
    
    public void DebugQueues(bool isBefore)
    {
        if (!ActivePlayers.Any())
        {
            Console.WriteLine($"{RetakesPlugin.LogPrefix}ActivePlayers ({(isBefore ? "BEFORE" : "AFTER")}): No active players.");
        }
        else
        {
            Console.WriteLine($"{RetakesPlugin.LogPrefix}ActivePlayers ({(isBefore ? "BEFORE" : "AFTER")}): {string.Join(", ", ActivePlayers.Where(Helpers.IsValidPlayer).Select(player => player.PlayerName))}");
        }

        if (!QueuePlayers.Any())
        {
            Console.WriteLine($"{RetakesPlugin.LogPrefix}QueuePlayers ({(isBefore ? "BEFORE" : "AFTER")}): No players in the queue.");
        }
        else
        {
            Console.WriteLine($"{RetakesPlugin.LogPrefix}QueuePlayers ({(isBefore ? "BEFORE" : "AFTER")}): {string.Join(", ", QueuePlayers.Where(Helpers.IsValidPlayer).Select(player => player.PlayerName))}");
        }
    }

    public void ClearRoundTeams()
    {
        RoundTerrorists.Clear();
        RoundCounterTerrorists.Clear();
    }
    
    public void SetRoundTeams()
    {
        RoundTerrorists = Utilities.GetPlayers().Where(player => Helpers.IsValidPlayer(player) && (CsTeam)player.TeamNum == CsTeam.Terrorist).ToList();
        RoundCounterTerrorists = Utilities.GetPlayers().Where(player => Helpers.IsValidPlayer(player) && (CsTeam)player.TeamNum == CsTeam.CounterTerrorist).ToList();
    }
}