using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Utils;

using RetakesPlugin.Managers;
using RetakesPlugin.Models;
using RetakesPlugin.Utils;
using RetakesPluginShared.Enums;

namespace RetakesPlugin.Commands.SpawnEditor;

public class NearestSpawnCommand
{
    private readonly RetakesPlugin _plugin;
    private readonly SpawnManager _spawnManager;
    private readonly ShowSpawnsCommand _showSpawnsCommand;

    public NearestSpawnCommand(RetakesPlugin plugin, SpawnManager spawnManager, ShowSpawnsCommand showSpawnsCommand)
    {
        _plugin = plugin;
        _spawnManager = spawnManager;
        _showSpawnsCommand = showSpawnsCommand;
    }

    public void OnCommand(CCSPlayerController? player, CommandInfo commandInfo)
    {
        if (!PlayerHelper.IsValid(player))
        {
            return;
        }

        if (!AdminManager.PlayerHasPermissions(player, "@css/root"))
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

        var spawns = _spawnManager.GetSpawns((Bombsite)_showSpawnsCommand.ShowingSpawnsForBombsite);

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