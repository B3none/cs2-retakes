using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;

using RetakesPlugin.Utils;
using RetakesPlugin.Events;

namespace RetakesPlugin.Commands.Admin;

public class ForceBombsiteStopCommand
{
    private readonly RetakesPlugin _plugin;
    private readonly RoundEventHandlers _roundEventHandlers;

    public ForceBombsiteStopCommand(RetakesPlugin plugin, RoundEventHandlers roundEventHandlers)
    {
        _plugin = plugin;
        _roundEventHandlers = roundEventHandlers;
    }

    public void OnCommand(CCSPlayerController? player, CommandInfo commandInfo)
    {
        if (player != null && !PlayerHelper.IsValid(player))
        {
            return;
        }

        var requiredPermission = PlayerHelper.GetCommandPermission(_plugin.Config, "css_forcebombsitestop", "Admin");
        if (!AdminManager.PlayerHasPermissions(player, requiredPermission))
        {
            commandInfo.ReplyToCommand($"{_plugin.Localizer["retakes.prefix"]} {_plugin.Localizer["retakes.no_permissions"]}");
            return;
        }

        _roundEventHandlers.SetForcedBombsite(null);

        commandInfo.ReplyToCommand($"{_plugin.Localizer["retakes.prefix"]} The bombsite will no longer be forced.");
        Logger.LogInfo("Commands", $"Forced bombsite cleared by {player?.PlayerName ?? "Console"}");
    }
}