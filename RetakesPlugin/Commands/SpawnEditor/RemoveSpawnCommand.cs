using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;

using RetakesPlugin.Utils;
using RetakesPlugin.Models;
using RetakesPlugin.Services;
using RetakesPlugin.Managers;
using RetakesPluginShared.Enums;

namespace RetakesPlugin.Commands.SpawnEditor;

public class RemoveSpawnCommand
{
    private readonly RetakesPlugin _plugin;
    private readonly ShowSpawnsCommand _showSpawnsCommand;

    public RemoveSpawnCommand(RetakesPlugin plugin, ShowSpawnsCommand showSpawnsCommand)
    {
        _plugin = plugin;
        _showSpawnsCommand = showSpawnsCommand;
    }

    public void OnCommand(CCSPlayerController? player, CommandInfo commandInfo)
    {
        if (!PlayerHelper.IsValid(player))
        {
            return;
        }

        var commandName = commandInfo.GetArg(0);
        var requiredPermission = PlayerHelper.GetCommandPermission(_plugin.Config, commandName, "SpawnEditor");
        if (!AdminManager.PlayerHasPermissions(player, requiredPermission))
        {
            commandInfo.ReplyToCommand($"{_plugin.Localizer["retakes.prefix"]} {_plugin.Localizer["retakes.no_permissions"]}");
            return;
        }

        if (_showSpawnsCommand.ShowingSpawnsForBombsite == null)
        {
            commandInfo.ReplyToCommand($"{_plugin.Localizer["retakes.prefix"]} You can't remove a spawn if you're not showing the spawns.");
            return;
        }

        if (!PlayerHelper.HasAlivePawn(player))
        {
            return;
        }

        if (_plugin.SpawnManager == null || _plugin.MapConfigService == null)
        {
            commandInfo.ReplyToCommand($"{_plugin.Localizer["retakes.prefix"]} Services not initialized.");
            return;
        }

        var spawns = _plugin.SpawnManager.GetSpawns((Bombsite)_showSpawnsCommand.ShowingSpawnsForBombsite);

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

        var didRemoveSpawn = _plugin.MapConfigService.RemoveSpawn(closestSpawn);
        if (didRemoveSpawn)
        {
            _plugin.SpawnManager.CalculateMapSpawns();
        }

        commandInfo.ReplyToCommand($"{_plugin.Localizer["retakes.prefix"]} {(didRemoveSpawn ? "Spawn removed" : "Error removing spawn")}");

        if (didRemoveSpawn)
        {
            Logger.LogInfo("Commands", $"{player?.PlayerName} removed spawn at bombsite {_showSpawnsCommand.ShowingSpawnsForBombsite}");
        }
    }
}