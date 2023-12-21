using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;

namespace RetakesPlugin.Modules.Managers;

public class Queue
{
    public const int MaxRetakesPlayers = 9;
    public const float TerroristRatio = 0.45f;

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
        return ActivePlayers.Count - GetNumTerrorists();
    }

    public void PlayerTriedToJoinTeam(CCSPlayerController player, bool switchToSpectator)
    {
        Console.WriteLine($"{RetakesPlugin.MessagePrefix}[{player.PlayerName}] PlayerTriedToJoinTeam called.");
        
        Console.WriteLine($"{RetakesPlugin.MessagePrefix}[{player.PlayerName}] Checking ActivePlayers.");
        if (ActivePlayers.Contains(player))
        {
            Console.WriteLine($"{RetakesPlugin.MessagePrefix}[{player.PlayerName}] Already an active player.");
            return;
        }

        Console.WriteLine($"{RetakesPlugin.MessagePrefix}[{player.PlayerName}] Checking QueuePlayers.");
        if (!QueuePlayers.Contains(player))
        {
            Console.WriteLine($"{RetakesPlugin.MessagePrefix}[{player.PlayerName}] Not found, adding to QueuePlayers.");
            QueuePlayers.Add(player);
        }
        else
        {
            Console.WriteLine($"{RetakesPlugin.MessagePrefix}[{player.PlayerName}] Found in QueuePlayers, do nothing.");
        }

        Console.WriteLine($"{RetakesPlugin.MessagePrefix}[{player.PlayerName}] Should switch to spectator? {(switchToSpectator ? "yes" : "no")}");
        if (switchToSpectator)
        {
            Console.WriteLine($"{RetakesPlugin.MessagePrefix}[{player.PlayerName}] Changing to spectator.");
            player.ChangeTeam(CsTeam.Spectator);
        }
    }

    private void RemoveDisconnectedPlayers()
    {
        var disconnectedActivePlayers = ActivePlayers.Where(player => !Helpers.IsPlayerConnected(player)).ToList();

        if (disconnectedActivePlayers.Count > 0)
        {
            Console.WriteLine($"{RetakesPlugin.MessagePrefix}Removing {disconnectedActivePlayers.Count} disconnected players from ActivePlayers.");
            ActivePlayers.RemoveAll(player => disconnectedActivePlayers.Contains(player));
        }
        
        var disconnectedQueuePlayers = QueuePlayers.Where(player => !Helpers.IsPlayerConnected(player)).ToList();
        
        if (disconnectedQueuePlayers.Count > 0)
        {
            Console.WriteLine($"{RetakesPlugin.MessagePrefix}Removing {disconnectedQueuePlayers.Count} disconnected players from QueuePlayers.");
            QueuePlayers.RemoveAll(player => disconnectedQueuePlayers.Contains(player));
        }
    }

    private void AddConnectedPlayers()
    {
        var connectedPlayers = Utilities.GetPlayers().Where(Helpers.IsPlayerConnected).ToList();

        foreach (var connectedPlayer in connectedPlayers)
        {
            if (!ActivePlayers.Contains(connectedPlayer) && !QueuePlayers.Contains(connectedPlayer))
            {
                Console.WriteLine($"{RetakesPlugin.MessagePrefix}Adding {connectedPlayer.PlayerName} to QueuePlayers.");
                QueuePlayers.Add(connectedPlayer);
            }
        }
    }
    
    public void Update()
    {
        RemoveDisconnectedPlayers();
        AddConnectedPlayers();
        
        var playersToAdd = MaxRetakesPlayers - ActivePlayers.Count;

        if (playersToAdd > 0 && QueuePlayers.Count > 0)
        {
            // Take players from QueuePlayers and add them to ActivePlayers
            var playersToAddList = QueuePlayers.Take(playersToAdd).ToList();

            // Remove the players that will be added from the Queue
            QueuePlayers.RemoveAll(player => playersToAddList.Contains(player));

            ActivePlayers.AddRange(playersToAddList);
        }
    }

    public void PlayerDisconnected(CCSPlayerController player)
    {
        if (ActivePlayers.Contains(player))
        {
            ActivePlayers.Remove(player);
        }

        if (QueuePlayers.Contains(player))
        {
            QueuePlayers.Remove(player);
        }
    }
    
    public void DebugQueues(bool isBefore)
    {
        if (ActivePlayers == null || !ActivePlayers.Any())
        {
            Console.WriteLine($"{RetakesPlugin.MessagePrefix}ActivePlayers ({(isBefore ? "BEFORE" : "AFTER")}): No active players.");
        }
        else
        {
            Console.WriteLine($"{RetakesPlugin.MessagePrefix}ActivePlayers ({(isBefore ? "BEFORE" : "AFTER")}): {string.Join(", ", ActivePlayers.Where(Helpers.IsValidPlayer).Select(player => player.PlayerName))}");
        }

        if (QueuePlayers == null || !QueuePlayers.Any())
        {
            Console.WriteLine($"{RetakesPlugin.MessagePrefix}QueuePlayers ({(isBefore ? "BEFORE" : "AFTER")}): No players in the queue.");
        }
        else
        {
            Console.WriteLine($"{RetakesPlugin.MessagePrefix}QueuePlayers ({(isBefore ? "BEFORE" : "AFTER")}): {string.Join(", ", QueuePlayers.Where(Helpers.IsValidPlayer).Select(player => player.PlayerName))}");
        }
    }
}