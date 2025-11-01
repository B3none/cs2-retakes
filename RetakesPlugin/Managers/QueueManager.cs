using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;

using RetakesPlugin.Utils;

namespace RetakesPlugin.Managers;

public class QueueManager
{
    private readonly RetakesPlugin _plugin;
    private readonly int _maxRetakesPlayers;
    private readonly float _terroristRatio;
    private readonly string[] _queuePriorityFlags;
    private readonly string[] _queueImmunityFlags;
    private readonly bool _shouldForceEvenTeamsWhenPlayerCountIsMultipleOf10;
    private readonly bool _shouldPreventTeamChangesMidRound;

    public HashSet<CCSPlayerController> QueuePlayers = [];
    public HashSet<CCSPlayerController> ActivePlayers = [];

    public QueueManager(RetakesPlugin plugin, int? retakesMaxPlayers, float? retakesTerroristRatio, string? queuePriorityFlags, string? queueImmunityFlags, bool? shouldForceEvenTeamsWhenPlayerCountIsMultipleOf10, bool? shouldPreventTeamChangesMidRound)
    {
        _plugin = plugin;
        _maxRetakesPlayers = retakesMaxPlayers ?? 9;
        _terroristRatio = retakesTerroristRatio ?? 0.45f;
        _queuePriorityFlags = ParseFlags(queuePriorityFlags);

        if (_queuePriorityFlags.Length == 0)
        {
            _queuePriorityFlags = ["@css/vip"];
        }

        var parsedImmunityFlags = ParseFlags(queueImmunityFlags);
        _queueImmunityFlags = parsedImmunityFlags.Length > 0 ? parsedImmunityFlags : _queuePriorityFlags;

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

            if (!_shouldPreventTeamChangesMidRound || GameRulesHelper.GetGameRules().WarmupPeriod)
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
            if (GameRulesHelper.GetGameRules().WarmupPeriod && ActivePlayers.Count < _maxRetakesPlayers)
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
            QueuePlayers.RemoveWhere(player => disconnectedQueuePlayers.Contains(player));
        }
    }

    private void HandleQueuePriority()
    {
        if (ActivePlayers.Count != _maxRetakesPlayers)
        {
            return;
        }

        var vipQueuePlayers = QueuePlayers
            .Where(player => PlayerHelper.HasQueuePriority(player, _queuePriorityFlags))
            .ToList();

        if (vipQueuePlayers.Count <= 0)
        {
            return;
        }

        foreach (var vipQueuePlayer in vipQueuePlayers)
        {
            if (!PlayerHelper.IsValid(vipQueuePlayer))
            {
                continue;
            }

            var replaceablePlayers = PlayerHelper.Shuffle(
                ActivePlayers
                    .Where(player => !PlayerHelper.HasQueuePriority(player, _queuePriorityFlags) && !PlayerHelper.HasQueueImmunity(player, _queueImmunityFlags))
                    .ToList(),
                new Random()
            );

            if (replaceablePlayers.Count == 0)
            {
                Logger.LogDebug("QueueManager", "No replaceable players found");
                break;
            }

            var replaceablePlayer = replaceablePlayers.First();

            replaceablePlayer.ChangeTeam(CsTeam.Spectator);
            ActivePlayers.Remove(replaceablePlayer);
            QueuePlayers.Add(replaceablePlayer);
            replaceablePlayer.PrintToChat($"{_plugin.Localizer["retakes.prefix"]} {_plugin.Localizer["retakes.queue.replaced_by_vip", vipQueuePlayer.PlayerName]}");

            ActivePlayers.Add(vipQueuePlayer);
            QueuePlayers.Remove(vipQueuePlayer);
            vipQueuePlayer.ChangeTeam(CsTeam.CounterTerrorist);
            vipQueuePlayer.PrintToChat($"{_plugin.Localizer["retakes.prefix"]} {_plugin.Localizer["retakes.queue.vip_took_place", replaceablePlayer.PlayerName]}");

            Logger.LogInfo("QueueManager", $"VIP {vipQueuePlayer.PlayerName} replaced {replaceablePlayer.PlayerName}");
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
                .OrderBy(player => PlayerHelper.HasQueuePriority(player, _queuePriorityFlags) ? 0 : 1)
                .Take(playersToAdd)
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

    private static string[] ParseFlags(string? rawFlags)
    {
        if (string.IsNullOrWhiteSpace(rawFlags))
        {
            return Array.Empty<string>();
        }

        return rawFlags
            .Split(",", StringSplitOptions.RemoveEmptyEntries)
            .Select(flag => flag.Trim())
            .Where(flag => !string.IsNullOrWhiteSpace(flag))
            .ToArray();
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