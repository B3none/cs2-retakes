using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Utils;

using RetakesPlugin.Utils;
using RetakesPlugin.Models;
using RetakesPlugin.Managers;
using RetakesPluginShared.Enums;

namespace RetakesPlugin.Commands.SpawnEditor;

public class NearestSpawnCommand
{
    private readonly RetakesPlugin _plugin;
    private readonly ShowSpawnsCommand _showSpawnsCommand;

    public NearestSpawnCommand(RetakesPlugin plugin, ShowSpawnsCommand showSpawnsCommand)
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
            commandInfo.ReplyToCommand($"{_plugin.Localizer["retakes.prefix"]} You must be in spawn editing mode.");
            return;
        }

        if (!PlayerHelper.HasAlivePawn(player))
        {
            return;
        }

        if (_plugin.SpawnManager == null)
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
}