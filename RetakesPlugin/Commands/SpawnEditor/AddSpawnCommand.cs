using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Utils;

using RetakesPlugin.Utils;
using RetakesPlugin.Models;
using RetakesPlugin.Services;
using RetakesPlugin.Managers;
using RetakesPluginShared.Enums;

namespace RetakesPlugin.Commands.SpawnEditor;

public class AddSpawnCommand
{
    private readonly RetakesPlugin _plugin;
    private readonly ShowSpawnsCommand _showSpawnsCommand;

    public AddSpawnCommand(RetakesPlugin plugin, ShowSpawnsCommand showSpawnsCommand)
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
            commandInfo.ReplyToCommand($"{_plugin.Localizer["retakes.prefix"]} You can't add a spawn if you're not showing the spawns.");
            return;
        }

        if (!PlayerHelper.HasAlivePawn(player))
        {
            commandInfo.ReplyToCommand($"{_plugin.Localizer["retakes.prefix"]} You must have an alive player pawn.");
            return;
        }

        if (commandInfo.ArgCount < 2)
        {
            commandInfo.ReplyToCommand($"{_plugin.Localizer["retakes.prefix"]} Usage: !add [T/CT] [Y/N can be planter]");
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

        if (_plugin.SpawnManager == null || _plugin.MapConfigService == null)
        {
            commandInfo.ReplyToCommand($"{_plugin.Localizer["retakes.prefix"]} Services not initialized.");
            return;
        }

        var spawns = _plugin.SpawnManager.GetSpawns((Bombsite)_showSpawnsCommand.ShowingSpawnsForBombsite);
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
            CanBePlanter = team == "T" && !string.IsNullOrWhiteSpace(canBePlanterInput) ? canBePlanterInput == "Y" : player.PlayerPawn.Value.InBombZoneTrigger,
            Bombsite = (Bombsite)_showSpawnsCommand.ShowingSpawnsForBombsite
        };

        SpawnService.ShowSpawn(newSpawn);

        var didAddSpawn = _plugin.MapConfigService.AddSpawn(newSpawn);
        if (didAddSpawn)
        {
            _plugin.SpawnManager.CalculateMapSpawns();
        }

        commandInfo.ReplyToCommand($"{_plugin.Localizer["retakes.prefix"]} {(didAddSpawn ? "Spawn added" : "Error adding spawn")}");

        if (didAddSpawn)
        {
            Logger.LogInfo("Commands", $"{player.PlayerName} added spawn at bombsite {_showSpawnsCommand.ShowingSpawnsForBombsite}");
        }
    }
}