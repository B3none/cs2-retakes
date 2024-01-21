using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Utils;

namespace RetakesPlugin.Modules.Managers;

public class QueueManager
{
    private readonly Translator _translator;
    private readonly int _maxRetakesPlayers;
    private readonly float _terroristRatio;

    public HashSet<CCSPlayerController> QueuePlayers = new();
    public HashSet<CCSPlayerController> ActivePlayers = new();

    public QueueManager(Translator translator, int? retakesMaxPlayers, float? retakesTerroristRatio)
    {
        _translator = translator;
        _maxRetakesPlayers = retakesMaxPlayers ?? 9;
        _terroristRatio = retakesTerroristRatio ?? 0.45f;
    }

    public int GetTargetNumTerrorists()
    {
        // TODO: Add a config option for this logic
        var ratio = (ActivePlayers.Count > 9 ? 0.5 : _terroristRatio) * ActivePlayers.Count;
        var numTerrorists = (int)Math.Round(ratio);

        // Ensure at least one terrorist if the calculated number is zero
        return numTerrorists > 0 ? numTerrorists : 1;
    }

    public int GetTargetNumCounterTerrorists()
    {
        return ActivePlayers.Count - GetTargetNumTerrorists();
    }

    public HookResult PlayerJoinedTeam(CCSPlayerController player, CsTeam fromTeam, CsTeam toTeam)
    {
        Console.WriteLine($"{RetakesPlugin.LogPrefix}[{player.PlayerName}] PlayerTriedToJoinTeam called.");

        if (
            fromTeam == CsTeam.None && toTeam == CsTeam.Spectator
            || fromTeam == CsTeam.Spectator && toTeam == CsTeam.None
            || fromTeam == toTeam && toTeam == CsTeam.None
        )
        {
            // This is called when a player first joins.
            Console.WriteLine(
                $"{RetakesPlugin.LogPrefix}[{player.PlayerName}] {fromTeam.ToString()} -> {toTeam.ToString()}.");
            return HookResult.Continue;
        }

        Console.WriteLine($"{RetakesPlugin.LogPrefix}[{player.PlayerName}] Checking ActivePlayers.");
        if (ActivePlayers.Contains(player))
        {
            Console.WriteLine($"{RetakesPlugin.LogPrefix}[{player.PlayerName}] Player is an active player.");

            if (toTeam == CsTeam.Spectator)
            {
                Console.WriteLine($"{RetakesPlugin.LogPrefix}[{player.PlayerName}] Switching to spectator.");
                RemovePlayerFromQueues(player);
                return HookResult.Continue;
            }

            if (
                _roundTerrorists.Count > 0
                && _roundCounterTerrorists.Count > 0
                && (
                    (toTeam == CsTeam.CounterTerrorist && !_roundCounterTerrorists.Contains(player))
                    || (toTeam == CsTeam.Terrorist && !_roundTerrorists.Contains(player))
                )
            )
            {
                Console.WriteLine($"{RetakesPlugin.LogPrefix}[{player.PlayerName}] player is not in round list for {toTeam}, switching to spectator.");
                ActivePlayers.Remove(player);
                QueuePlayers.Add(player);

                // TODO: This might not be needed
                if (player.PawnIsAlive)
                {
                    player.CommitSuicide(false, true);
                }
                
                player.ChangeTeam(CsTeam.Spectator);
                return HookResult.Handled;
            }

            Console.WriteLine($"{RetakesPlugin.LogPrefix}[{player.PlayerName}] The player tried joining the team they're already on, or, there were not enough players so we don't care. Do nothing.");
            return HookResult.Handled;
        }

        Console.WriteLine($"{RetakesPlugin.LogPrefix}[{player.PlayerName}] Checking QueuePlayers.");
        if (!QueuePlayers.Contains(player))
        {
            if (Helpers.GetGameRules().WarmupPeriod && ActivePlayers.Count < _maxRetakesPlayers)
            {
                Console.WriteLine($"{RetakesPlugin.LogPrefix}[{player.PlayerName}] Not found, adding to ActivePlayers (because in warmup).");
                ActivePlayers.Add(player);
                return HookResult.Continue;
            }

            Console.WriteLine($"{RetakesPlugin.LogPrefix}[{player.PlayerName}] Not found, adding to QueuePlayers.");
            player.PrintToChat($"{RetakesPlugin.MessagePrefix}{_translator["queue.joined"]}");
            QueuePlayers.Add(player);
            return HookResult.Handled;
        }
        else
        {
            Console.WriteLine($"{RetakesPlugin.LogPrefix}[{player.PlayerName}] Already in Queue, do nothing.");
        }

        Helpers.CheckRoundDone();
        return HookResult.Handled;
    }

    private void RemoveDisconnectedPlayers()
    {
        var disconnectedActivePlayers = ActivePlayers
            .Where(player => !Helpers.IsValidPlayer(player) || !Helpers.IsPlayerConnected(player)).ToList();

        if (disconnectedActivePlayers.Count > 0)
        {
            Console.WriteLine(
                $"{RetakesPlugin.LogPrefix}Removing {disconnectedActivePlayers.Count} disconnected players from ActivePlayers.");
            ActivePlayers.RemoveWhere(player => disconnectedActivePlayers.Contains(player));
        }

        var disconnectedQueuePlayers = QueuePlayers
            .Where(player => !Helpers.IsValidPlayer(player) || !Helpers.IsPlayerConnected(player)).ToList();

        if (disconnectedQueuePlayers.Count > 0)
        {
            Console.WriteLine(
                $"{RetakesPlugin.LogPrefix}Removing {disconnectedQueuePlayers.Count} disconnected players from QueuePlayers.");
            QueuePlayers.RemoveWhere(player => disconnectedQueuePlayers.Contains(player));
        }
    }

    private void HandleQueuePriority()
    {
        var vipsInQueue = QueuePlayers.Where(player => AdminManager.PlayerHasPermissions(player, "@css/vip")).ToList()
            .Count;

        if (vipsInQueue > 0)
        {
            Helpers.Shuffle(
                    ActivePlayers
                        .Where(player => !AdminManager.PlayerHasPermissions(player, "@css/vip"))
                        .ToList()
                )
                .ForEach(player =>
                {
                    if (vipsInQueue <= 0)
                    {
                        return;
                    }

                    ActivePlayers.Remove(player);
                    QueuePlayers.Add(player);
                    vipsInQueue--;
                });
        }
    }

    public void Update()
    {
        RemoveDisconnectedPlayers();
        HandleQueuePriority();

        var playersToAdd = _maxRetakesPlayers - ActivePlayers.Count;

        if (playersToAdd > 0 && QueuePlayers.Count > 0)
        {
            // Take players from QueuePlayers and add them to ActivePlayers
            // Ordered by players with @retakes/queue group first since they
            // have queue priority.
            var playersToAddList = QueuePlayers
                .OrderBy(player => AdminManager.PlayerHasPermissions(player, "@css/vip"))
                .Take(playersToAdd)
                .ToList();

            QueuePlayers.RemoveWhere(playersToAddList.Contains);

            // loop players to add, and set their team to CT
            foreach (var player in playersToAddList)
            {
                // If the player is no longer valid, skip them
                if (!Helpers.IsValidPlayer(player))
                {
                    continue;
                }

                ActivePlayers.Add(player);

                if (player.Team != CsTeam.CounterTerrorist)
                {
                    player.SwitchTeam(CsTeam.CounterTerrorist);
                }
            }
        }

        if (ActivePlayers.Count == _maxRetakesPlayers && QueuePlayers.Count > 0)
        {
            var waitingMessage = _translator["queue.waiting", ActivePlayers.Count];

            foreach (var player in QueuePlayers)
            {
                player.PrintToChat($"{RetakesPlugin.MessagePrefix}{waitingMessage}");
            }
        }
    }

    public void RemovePlayerFromQueues(CCSPlayerController player)
    {
        ActivePlayers.Remove(player);
        QueuePlayers.Remove(player);
        _roundTerrorists.Remove(player);
        _roundCounterTerrorists.Remove(player);

        Helpers.CheckRoundDone();
    }

    public void DebugQueues(bool isBefore)
    {
        if (!ActivePlayers.Any())
        {
            Console.WriteLine(
                $"{RetakesPlugin.LogPrefix}ActivePlayers ({(isBefore ? "BEFORE" : "AFTER")}): No active players.");
        }
        else
        {
            Console.WriteLine(
                $"{RetakesPlugin.LogPrefix}ActivePlayers ({(isBefore ? "BEFORE" : "AFTER")}): {string.Join(", ", ActivePlayers.Where(Helpers.IsValidPlayer).Select(player => player.PlayerName))}");
        }

        if (!QueuePlayers.Any())
        {
            Console.WriteLine(
                $"{RetakesPlugin.LogPrefix}QueuePlayers ({(isBefore ? "BEFORE" : "AFTER")}): No players in the queue.");
        }
        else
        {
            Console.WriteLine(
                $"{RetakesPlugin.LogPrefix}QueuePlayers ({(isBefore ? "BEFORE" : "AFTER")}): {string.Join(", ", QueuePlayers.Where(Helpers.IsValidPlayer).Select(player => player.PlayerName))}");
        }

        if (!_roundTerrorists.Any())
        {
            Console.WriteLine(
                $"{RetakesPlugin.LogPrefix}_roundTerrorists ({(isBefore ? "BEFORE" : "AFTER")}): No players in the queue.");
        }
        else
        {
            Console.WriteLine(
                $"{RetakesPlugin.LogPrefix}_roundTerrorists ({(isBefore ? "BEFORE" : "AFTER")}): {string.Join(", ", _roundTerrorists.Where(Helpers.IsValidPlayer).Select(player => player.PlayerName))}");
        }

        if (!_roundCounterTerrorists.Any())
        {
            Console.WriteLine(
                $"{RetakesPlugin.LogPrefix}_roundCounterTerrorists ({(isBefore ? "BEFORE" : "AFTER")}): No players in the queue.");
        }
        else
        {
            Console.WriteLine(
                $"{RetakesPlugin.LogPrefix}_roundCounterTerrorists ({(isBefore ? "BEFORE" : "AFTER")}): {string.Join(", ", _roundCounterTerrorists.Where(Helpers.IsValidPlayer).Select(player => player.PlayerName))}");
        }
    }

    private List<CCSPlayerController> _roundTerrorists = new();
    private List<CCSPlayerController> _roundCounterTerrorists = new();

    public void ClearRoundTeams()
    {
        _roundTerrorists.Clear();
        _roundCounterTerrorists.Clear();
    }

    public void SetRoundTeams()
    {
        _roundTerrorists = Utilities.GetPlayers()
            .Where(player => Helpers.IsValidPlayer(player) && player.Team == CsTeam.Terrorist).ToList();
        _roundCounterTerrorists = Utilities.GetPlayers()
            .Where(player => Helpers.IsValidPlayer(player) && player.Team == CsTeam.CounterTerrorist).ToList();
    }
}