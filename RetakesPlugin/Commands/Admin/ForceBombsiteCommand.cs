using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;

using RetakesPlugin.Events;
using RetakesPlugin.Utils;
using RetakesPluginShared.Enums;

namespace RetakesPlugin.Commands.Admin;

public class ForceBombsiteCommand
{
    private readonly RetakesPlugin _plugin;
    private readonly RoundEventHandlers _roundEventHandlers;

    public ForceBombsiteCommand(RetakesPlugin plugin, RoundEventHandlers roundEventHandlers)
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

        if (!AdminManager.PlayerHasPermissions(player, "@css/root"))
        {
            commandInfo.ReplyToCommand($"{_plugin.Localizer["retakes.prefix"]} {_plugin.Localizer["retakes.no_permissions"]}");
            return;
        }

        if (commandInfo.ArgCount < 2)
        {
            commandInfo.ReplyToCommand($"{_plugin.Localizer["retakes.prefix"]} Usage: !forcebombsite [A/B]");
            return;
        }

        var bombsite = commandInfo.GetArg(1).ToUpper();
        if (bombsite != "A" && bombsite != "B")
        {
            commandInfo.ReplyToCommand($"{_plugin.Localizer["retakes.prefix"]} You must specify a bombsite [A / B].");
            return;
        }

        var forcedBombsite = bombsite == "A" ? Bombsite.A : Bombsite.B;
        _roundEventHandlers.SetForcedBombsite(forcedBombsite);

        commandInfo.ReplyToCommand($"{_plugin.Localizer["retakes.prefix"]} The bombsite will now be forced to {forcedBombsite}.");
        Logger.LogInfo("Commands", $"Bombsite forced to {forcedBombsite} by {player?.PlayerName ?? "Console"}");
    }
}