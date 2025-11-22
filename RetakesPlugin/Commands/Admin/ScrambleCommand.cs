using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;

using RetakesPlugin.Utils;
using RetakesPlugin.Managers;

namespace RetakesPlugin.Commands.Admin;

public class ScrambleCommand
{
    private readonly RetakesPlugin _plugin;
    private readonly GameManager _gameManager;

    public ScrambleCommand(RetakesPlugin plugin, GameManager gameManager)
    {
        _plugin = plugin;
        _gameManager = gameManager;
    }

    public void OnCommand(CCSPlayerController? player, CommandInfo commandInfo)
    {
        if (player != null && !PlayerHelper.IsValid(player))
        {
            return;
        }

        var commandName = commandInfo.GetArg(0);
        var requiredPermission = PlayerHelper.GetCommandPermission(_plugin.Config, commandName, "Admin", "@css/admin");
        if (!AdminManager.PlayerHasPermissions(player, requiredPermission))
        {
            commandInfo.ReplyToCommand($"{_plugin.Localizer["retakes.prefix"]} {_plugin.Localizer["retakes.no_permissions"]}");
            return;
        }

        _gameManager.ScrambleNextRound(player);
        Logger.LogInfo("Commands", $"Teams scramble requested by {player?.PlayerName ?? "Console"}");
    }
}