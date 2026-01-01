using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;

using RetakesPlugin.Utils;
using RetakesPlugin.Configs;

namespace RetakesPlugin.Managers;

public class QueueManager
{
    private readonly RetakesPlugin _plugin;
    private readonly int _maxRetakesPlayers;
    private readonly float _terroristRatio;
    private readonly List<QueuePriorityFlagConfig> _queuePriorityFlags;
    private readonly List<QueuePriorityFlagConfig> _queueImmunityFlags;
    private readonly bool _shouldForceEvenTeamsWhenPlayerCountIsMultipleOf10;
    private readonly bool _shouldPreventTeamChangesMidRound;

    public HashSet<CCSPlayerController> QueuePlayers = [];
    public HashSet<CCSPlayerController> ActivePlayers = [];

    public QueueManager(RetakesPlugin plugin, int? retakesMaxPlayers, float? retakesTerroristRatio, List<QueuePriorityFlagConfig>? queuePriorityFlags, List<QueuePriorityFlagConfig>? queueImmunityFlags, bool? shouldForceEvenTeamsWhenPlayerCountIsMultipleOf10, bool? shouldPreventTeamChangesMidRound)
    {
        _plugin = plugin;
        _maxRetakesPlayers = retakesMaxPlayers ?? 9;
        _terroristRatio = retakesTerroristRatio ?? 0.45f;

        _queuePriorityFlags = queuePriorityFlags ?? new List<QueuePriorityFlagConfig>();
        _queueImmunityFlags = queueImmunityFlags ?? new List<QueuePriorityFlagConfig>();

        _shouldForceEvenTeamsWhenPlayerCountIsMultipleOf10 = shouldForceEvenTeamsWhenPlayerCountIsMultipleOf10 ?? true;
        _shouldPreventTeamChangesMidRound = shouldPreventTeamChangesMidRound ?? true;

        Logger.LogInfo("QueueManager", $"Queue manager initialized (Max: {_maxRetakesPlayers}, T Ratio: {_terroristRatio})");
    }

    public int GetTargetNumTerrorists()
    {
        var shouldForceEvenTeams = _shouldForceEvenTeamsWhenPlayerCountIsMultipleOf10 && ActivePlayers.Count % 10 == 0;
        var ratio = (shouldForceEvenTeams ? 0.5 : _terroristRatio) * ActivePlayers.Count;
        var numTerrorists = (int)Math.Round(ratio);

        return numTerrorists > 0 ? numTerrorists : 1;
    }

    public int GetTargetNumCounterTerrorists()
    {
        return ActivePlayers.Count - GetTargetNumTerrorists();
    }

    public HookResult PlayerJoinedTeam(CCSPlayerController player, CsTeam fromTeam, CsTeam toTeam)
    {
        Logger.LogDebug("QueueManager", $"[{player.PlayerName}] Team change: {fromTeam} -> {toTeam}");

        if (fromTeam == CsTeam.None && toTeam == CsTeam.Spectator)
        {
            return HookResult.Continue;
        }

        if (ActivePlayers.Contains(player))
        {
            Logger.LogDebug("QueueManager", $"[{player.PlayerName}] Player is active");

            if (toTeam == CsTeam.Spectator)
            {
                Logger.LogInfo("QueueManager", $"[{player.PlayerName}] Switched to spectator");
                RemovePlayerFromQueues(player);
                GameRulesHelper.CheckRoundDone();
                return HookResult.Continue;
            }

            var gameRules = GameRulesHelper.GetGameRulesOrNull();
            if (!_shouldPreventTeamChangesMidRound || (gameRules?.WarmupPeriod ?? false))
            {
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
                Logger.LogInfo("QueueManager", $"[{player.PlayerName}] Prevented mid-round team change");
                ActivePlayers.Remove(player);
                QueuePlayers.Add(player);

                if (player.PawnIsAlive)
                {
                    player.CommitSuicide(false, true);
                }

                player.ChangeTeam(CsTeam.Spectator);
                return HookResult.Handled;
            }

            GameRulesHelper.CheckRoundDone();
            return HookResult.Handled;
        }

        if (!QueuePlayers.Contains(player))
        {
            var gameRules = GameRulesHelper.GetGameRulesOrNull();
            if ((gameRules?.WarmupPeriod ?? false) && ActivePlayers.Count < _maxRetakesPlayers)
            {
                Logger.LogInfo("QueueManager", $"[{player.PlayerName}] Added to active players (warmup)");
                ActivePlayers.Add(player);
                return HookResult.Continue;
            }

            Logger.LogInfo("QueueManager", $"[{player.PlayerName}] Added to queue");
            player.PrintToChat($"{_plugin.Localizer["retakes.prefix"]} {_plugin.Localizer["retakes.queue.joined"]}");
            QueuePlayers.Add(player);
        }

        GameRulesHelper.CheckRoundDone();
        return HookResult.Handled;
    }

    private void RemoveDisconnectedPlayers()
    {
        var disconnectedActivePlayers = ActivePlayers
            .Where(player => !PlayerHelper.IsValid(player) || !PlayerHelper.IsConnected(player))
            .ToList();

        if (disconnectedActivePlayers.Count > 0)
        {
            Logger.LogDebug("QueueManager", $"Removing {disconnectedActivePlayers.Count} disconnected active players");
            ActivePlayers.RemoveWhere(disconnectedActivePlayers.Contains);
        }

        var disconnectedQueuePlayers = QueuePlayers
            .Where(player => !PlayerHelper.IsValid(player) || !PlayerHelper.IsConnected(player))
            .ToList();

        if (disconnectedQueuePlayers.Count > 0)
        {
            Logger.LogDebug("QueueManager", $"Removing {disconnectedQueuePlayers.Count} disconnected queue players");
            QueuePlayers.RemoveWhere(disconnectedQueuePlayers.Contains);
        }
    }

    private void HandleQueuePriority()
    {
        if (ActivePlayers.Count != _maxRetakesPlayers)
        {
            return;
        }

        var queuePlayersWithPriority = QueuePlayers
            .Where(PlayerHelper.IsValid)
            .Select(player => new
            {
                Player = player,
                Priority = PlayerHelper.GetQueuePriority(player, _queuePriorityFlags)
            })
            .Where(x => x.Priority > int.MinValue)
            .OrderByDescending(x => x.Priority)
            .ThenBy(x => x.Player.Slot)
            .ToList();

        if (queuePlayersWithPriority.Count <= 0)
        {
            return;
        }

        foreach (var queuePlayerData in queuePlayersWithPriority)
        {
            var queuePlayer = queuePlayerData.Player;
            var queuePlayerPriority = queuePlayerData.Priority;

            if (!PlayerHelper.IsValid(queuePlayer))
            {
                continue;
            }

            var replaceablePlayers = ActivePlayers
                .Where(PlayerHelper.IsValid)
                .Select(player => new
                {
                    Player = player,
                    Priority = PlayerHelper.GetQueuePriority(player, _queuePriorityFlags),
                    ImmunityPriority = PlayerHelper.GetQueueImmunityPriority(player, _queueImmunityFlags)
                })
                .Where(x =>
                    x.Priority < queuePlayerPriority &&
                    x.ImmunityPriority < queuePlayerPriority)
                .OrderBy(x => x.Priority)
                .ThenByDescending(x => x.Player.Slot)
                .ToList();

            if (replaceablePlayers.Count == 0)
            {
                Logger.LogDebug("QueueManager", $"No replaceable players found for {queuePlayer.PlayerName} (priority: {queuePlayerPriority})");
                continue;
            }

            var replaceablePlayerData = replaceablePlayers.First();
            var replaceablePlayer = replaceablePlayerData.Player;

            var queuePlayerDisplayName = PlayerHelper.GetQueuePriorityDisplayName(queuePlayer, _queuePriorityFlags);

            replaceablePlayer.ChangeTeam(CsTeam.Spectator);
            ActivePlayers.Remove(replaceablePlayer);
            QueuePlayers.Add(replaceablePlayer);
            replaceablePlayer.PrintToChat($"{_plugin.Localizer["retakes.prefix"]} {_plugin.Localizer["retakes.queue.replaced_by_vip", queuePlayer.PlayerName, queuePlayerDisplayName]}");

            ActivePlayers.Add(queuePlayer);
            QueuePlayers.Remove(queuePlayer);
            queuePlayer.ChangeTeam(CsTeam.CounterTerrorist);
            queuePlayer.PrintToChat($"{_plugin.Localizer["retakes.prefix"]} {_plugin.Localizer["retakes.queue.vip_took_place", replaceablePlayer.PlayerName, queuePlayerDisplayName]}");

            Logger.LogInfo("QueueManager", $"{queuePlayer.PlayerName} ({queuePlayerDisplayName}, priority: {queuePlayerPriority}) replaced {replaceablePlayer.PlayerName} (priority: {replaceablePlayerData.Priority})");
        }
    }

    public void Update()
    {
        RemoveDisconnectedPlayers();

        Logger.LogDebug("QueueManager", $"Update: Max={_maxRetakesPlayers}, Active={ActivePlayers.Count}, Queue={QueuePlayers.Count}");

        var playersToAdd = _maxRetakesPlayers - ActivePlayers.Count;
        if (playersToAdd > 0 && QueuePlayers.Count > 0)
        {
            var playersToAddList = QueuePlayers
                .Where(PlayerHelper.IsValid)
                .Select(player => new
                {
                    Player = player,
                    Priority = PlayerHelper.GetQueuePriority(player, _queuePriorityFlags)
                })
                .OrderByDescending(x => x.Priority)
                .ThenBy(x => x.Player.Slot)
                .Take(playersToAdd)
                .Select(x => x.Player)
                .ToList();

            QueuePlayers.RemoveWhere(playersToAddList.Contains);

            foreach (var player in playersToAddList)
            {
                if (!PlayerHelper.IsValid(player))
                {
                    continue;
                }

                ActivePlayers.Add(player);
                player.ChangeTeam(CsTeam.CounterTerrorist);
                Logger.LogInfo("QueueManager", $"Moved {player.PlayerName} from queue to active");
            }
        }

        HandleQueuePriority();

        if (ActivePlayers.Count == _maxRetakesPlayers && QueuePlayers.Count > 0)
        {
            var waitingMessage = _plugin.Localizer["retakes.queue.waiting", ActivePlayers.Count];
            foreach (var player in QueuePlayers)
            {
                if (!PlayerHelper.IsValid(player))
                {
                    continue;
                }

                player.PrintToChat($"{_plugin.Localizer["retakes.prefix"]} {waitingMessage}");
            }
        }
    }

    public void RemovePlayerFromQueues(CCSPlayerController player)
    {
        ActivePlayers.Remove(player);
        QueuePlayers.Remove(player);
        _roundTerrorists.Remove(player);
        _roundCounterTerrorists.Remove(player);

        Logger.LogDebug("QueueManager", $"Removed {player.PlayerName} from all queues");
        GameRulesHelper.CheckRoundDone();
    }

    public void DebugQueues(bool isBefore)
    {
        var prefix = isBefore ? "BEFORE" : "AFTER";

        if (!ActivePlayers.Any())
        {
            Logger.LogDebug("QueueManager", $"ActivePlayers ({prefix}): Empty");
        }
        else
        {
            var names = string.Join(", ", ActivePlayers.Where(PlayerHelper.IsValid).Select(p => p.PlayerName));
            Logger.LogDebug("QueueManager", $"ActivePlayers ({prefix}): {names}");
        }

        if (!QueuePlayers.Any())
        {
            Logger.LogDebug("QueueManager", $"QueuePlayers ({prefix}): Empty");
        }
        else
        {
            var names = string.Join(", ", QueuePlayers.Where(PlayerHelper.IsValid).Select(p => p.PlayerName));
            Logger.LogDebug("QueueManager", $"QueuePlayers ({prefix}): {names}");
        }
    }

    private List<CCSPlayerController> _roundTerrorists = [];
    private List<CCSPlayerController> _roundCounterTerrorists = [];

    public void ClearRoundTeams()
    {
        _roundTerrorists.Clear();
        _roundCounterTerrorists.Clear();
        Logger.LogDebug("QueueManager", "Round teams cleared");
    }

    public void SetRoundTeams()
    {
        if (!_shouldPreventTeamChangesMidRound)
        {
            return;
        }

        _roundTerrorists = ActivePlayers
            .Where(player => PlayerHelper.IsValid(player) && player.Team == CsTeam.Terrorist)
            .ToList();

        _roundCounterTerrorists = ActivePlayers
            .Where(player => PlayerHelper.IsValid(player) && player.Team == CsTeam.CounterTerrorist)
            .ToList();

        Logger.LogDebug("QueueManager", $"Round teams set: {_roundTerrorists.Count} T, {_roundCounterTerrorists.Count} CT");
    }
}