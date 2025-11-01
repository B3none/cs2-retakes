using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Utils;

using RetakesPlugin.Managers;
using RetakesPlugin.Models;
using RetakesPlugin.Services;
using RetakesPlugin.Utils;
using RetakesPluginShared.Enums;

namespace RetakesPlugin.Commands;

public class SpawnEditorCommands
{
    private readonly RetakesPlugin _plugin;
    private readonly MapConfigService _mapConfigService;
    private readonly SpawnManager _spawnManager;
    private Bombsite? _showingSpawnsForBombsite;

    public SpawnEditorCommands(RetakesPlugin plugin, MapConfigService mapConfigService, SpawnManager spawnManager)
    {
        _plugin = plugin;
        _mapConfigService = mapConfigService;
        _spawnManager = spawnManager;
    }

    [ConsoleCommand("css_showspawns", "Show the spawns for the specified bombsite.")]
    [ConsoleCommand("css_spawns", "Show the spawns for the specified bombsite.")]
    [ConsoleCommand("css_edit", "Show the spawns for the specified bombsite.")]
    [CommandHelper(minArgs: 1, usage: "[A/B]", whoCanExecute: CommandUsage.CLIENT_ONLY)]
    [RequiresPermissions("@css/root")]
    public void OnCommandShowSpawns(CCSPlayerController? player, CommandInfo commandInfo)
    {
        if (!PlayerHelper.IsValid(player))
        {
            return;
        }

        var bombsite = commandInfo.GetArg(1).ToUpper();
        if (bombsite != "A" && bombsite != "B")
        {
            commandInfo.ReplyToCommand($"{_plugin.Localizer["retakes.prefix"]} You must specify a bombsite [A / B].");
            return;
        }

        _showingSpawnsForBombsite = bombsite == "A" ? Bombsite.A : Bombsite.B;

        Server.ExecuteCommand("mp_warmup_start");
        Server.ExecuteCommand("mp_warmuptime 120");
        Server.ExecuteCommand("mp_warmup_pausetimer 1");

        SpawnService.ShowSpawns(_plugin, _mapConfigService.GetSpawnsClone(), _showingSpawnsForBombsite);

        Logger.LogInfo("Commands", $"Showing spawns for bombsite {_showingSpawnsForBombsite} to {player.PlayerName}");
    }

    [ConsoleCommand("css_add", "Creates a new retakes spawn for the bombsite currently shown.")]
    [ConsoleCommand("css_addspawn", "Creates a new retakes spawn for the bombsite currently shown.")]
    [ConsoleCommand("css_new", "Creates a new retakes spawn for the bombsite currently shown.")]
    [ConsoleCommand("css_newspawn", "Creates a new retakes spawn for the bombsite currently shown.")]
    [CommandHelper(minArgs: 1, usage: "[T/CT] [Y/N can be planter]", whoCanExecute: CommandUsage.CLIENT_ONLY)]
    [RequiresPermissions("@css/root")]
    public void OnCommandAddSpawn(CCSPlayerController? player, CommandInfo commandInfo)
    {
        if (_showingSpawnsForBombsite == null)
        {
            commandInfo.ReplyToCommand($"{_plugin.Localizer["retakes.prefix"]} You can't add a spawn if you're not showing the spawns.");
            return;
        }

        if (!PlayerHelper.HasAlivePawn(player))
        {
            commandInfo.ReplyToCommand($"{_plugin.Localizer["retakes.prefix"]} You must have an alive player pawn.");
            return;
        }

        var team = commandInfo.GetArg(1).ToUpper();
        if (team != "T" && team != "CT")
        {
            commandInfo.ReplyToCommand($"{_plugin.Localizer["retakes.prefix"]} You must specify a team [T / CT].");
            return;
        }

        var canBePlanterInput = commandInfo.GetArg(2).ToUpper();
        if (!string.IsNullOrWhiteSpace(canBePlanterInput) && canBePlanterInput != "Y" && canBePlanterInput != "N")
        {
            commandInfo.ReplyToCommand($"{_plugin.Localizer["retakes.prefix"]} Incorrect value for can be planter [Y / N].");
            return;
        }

        var spawns = _spawnManager.GetSpawns((Bombsite)_showingSpawnsForBombsite);
        var closestDistance = 9999.9;

        foreach (var spawn in spawns)
        {
            var distance = GameRulesHelper.GetDistanceBetweenVectors(spawn.Vector, player!.PlayerPawn.Value!.AbsOrigin!);

            if (distance > 128.0 || distance > closestDistance)
            {
                continue;
            }

            closestDistance = distance;
        }

        if (closestDistance <= 72)
        {
            commandInfo.ReplyToCommand($"{_plugin.Localizer["retakes.prefix"]} You are too close to another spawn.");
            return;
        }

        var newSpawn = new Spawn(
            vector: player!.PlayerPawn.Value!.AbsOrigin!,
            qAngle: player!.PlayerPawn.Value!.AbsRotation!
        )
        {
            Team = team == "T" ? CsTeam.Terrorist : CsTeam.CounterTerrorist,
            CanBePlanter = team == "T" && !string.IsNullOrWhiteSpace(canBePlanterInput)
                ? canBePlanterInput == "Y"
                : player.PlayerPawn.Value.InBombZoneTrigger,
            Bombsite = (Bombsite)_showingSpawnsForBombsite
        };

        SpawnService.ShowSpawn(newSpawn);

        var didAddSpawn = _mapConfigService.AddSpawn(newSpawn);
        if (didAddSpawn)
        {
            _spawnManager.CalculateMapSpawns();
        }

        commandInfo.ReplyToCommand($"{_plugin.Localizer["retakes.prefix"]} {(didAddSpawn ? "Spawn added" : "Error adding spawn")}");

        if (didAddSpawn)
        {
            Logger.LogInfo("Commands", $"{player.PlayerName} added spawn at bombsite {_showingSpawnsForBombsite}");
        }
    }

    [ConsoleCommand("css_remove", "Deletes the nearest retakes spawn.")]
    [ConsoleCommand("css_removespawn", "Deletes the nearest retakes spawn.")]
    [ConsoleCommand("css_delete", "Deletes the nearest retakes spawn.")]
    [ConsoleCommand("css_deletespawn", "Deletes the nearest retakes spawn.")]
    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
    [RequiresPermissions("@css/root")]
    public void OnCommandRemoveSpawn(CCSPlayerController? player, CommandInfo commandInfo)
    {
        if (_showingSpawnsForBombsite == null)
        {
            commandInfo.ReplyToCommand($"{_plugin.Localizer["retakes.prefix"]} You can't remove a spawn if you're not showing the spawns.");
            return;
        }

        if (!PlayerHelper.HasAlivePawn(player))
        {
            return;
        }

        var spawns = _spawnManager.GetSpawns((Bombsite)_showingSpawnsForBombsite);

        if (spawns.Count == 0)
        {
            commandInfo.ReplyToCommand($"{_plugin.Localizer["retakes.prefix"]} No spawns found.");
            return;
        }

        var closestDistance = 9999.9;
        Spawn? closestSpawn = null;

        foreach (var spawn in spawns)
        {
            var distance = GameRulesHelper.GetDistanceBetweenVectors(spawn.Vector, player!.PlayerPawn.Value!.AbsOrigin!);

            if (distance > 128.0 || distance > closestDistance)
            {
                continue;
            }

            closestDistance = distance;
            closestSpawn = spawn;
        }

        if (closestSpawn == null)
        {
            commandInfo.ReplyToCommand($"{_plugin.Localizer["retakes.prefix"]} No spawns found within 128 units.");
            return;
        }

        SpawnService.RemoveSpawnBeam(closestSpawn);

        var didRemoveSpawn = _mapConfigService.RemoveSpawn(closestSpawn);
        if (didRemoveSpawn)
        {
            _spawnManager.CalculateMapSpawns();
        }

        commandInfo.ReplyToCommand($"{_plugin.Localizer["retakes.prefix"]} {(didRemoveSpawn ? "Spawn removed" : "Error removing spawn")}");

        if (didRemoveSpawn)
        {
            Logger.LogInfo("Commands", $"{player?.PlayerName} removed spawn at bombsite {_showingSpawnsForBombsite}");
        }
    }

    [ConsoleCommand("css_nearestspawn", "Goes to nearest retakes spawn.")]
    [ConsoleCommand("css_nearest", "Goes to nearest retakes spawn.")]
    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
    [RequiresPermissions("@css/root")]
    public void OnCommandNearestSpawn(CCSPlayerController? player, CommandInfo commandInfo)
    {
        if (_showingSpawnsForBombsite == null)
        {
            commandInfo.ReplyToCommand($"{_plugin.Localizer["retakes.prefix"]} You must be in spawn editing mode.");
            return;
        }

        if (!PlayerHelper.HasAlivePawn(player))
        {
            return;
        }

        var spawns = _spawnManager.GetSpawns((Bombsite)_showingSpawnsForBombsite);

        if (spawns.Count == 0)
        {
            commandInfo.ReplyToCommand($"{_plugin.Localizer["retakes.prefix"]} No spawns found.");
            return;
        }

        var closestDistance = 9999.9;
        Spawn? closestSpawn = null;

        foreach (var spawn in spawns)
        {
            var distance = GameRulesHelper.GetDistanceBetweenVectors(spawn.Vector, player!.PlayerPawn.Value!.AbsOrigin!);

            if (distance > closestDistance)
            {
                continue;
            }

            closestDistance = distance;
            closestSpawn = spawn;
        }

        if (closestSpawn == null)
        {
            commandInfo.ReplyToCommand($"{_plugin.Localizer["retakes.prefix"]} No spawns found.");
            return;
        }

        player!.PlayerPawn.Value!.Teleport(closestSpawn.Vector, closestSpawn.QAngle, new Vector());
        commandInfo.ReplyToCommand($"{_plugin.Localizer["retakes.prefix"]} Teleported to nearest spawn");
    }

    [ConsoleCommand("css_hidespawns", "Exits the spawn editing mode.")]
    [ConsoleCommand("css_done", "Exits the spawn editing mode.")]
    [ConsoleCommand("css_exitedit", "Exits the spawn editing mode.")]
    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
    [RequiresPermissions("@css/root")]
    public void OnCommandHideSpawns(CCSPlayerController? player, CommandInfo commandInfo)
    {
        _showingSpawnsForBombsite = null;
        Server.ExecuteCommand("mp_warmup_end");
        Logger.LogInfo("Commands", $"{player?.PlayerName} exited spawn editing mode");
    }
}