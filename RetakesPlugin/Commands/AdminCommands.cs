using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;

using RetakesPlugin.Events;
using RetakesPlugin.Managers;
using RetakesPlugin.Utils;
using RetakesPluginShared.Enums;

namespace RetakesPlugin.Commands;

public class AdminCommands
{
    private readonly RetakesPlugin _plugin;
    private readonly GameManager _gameManager;
    private readonly RoundEventHandlers _roundEventHandlers;

    public AdminCommands(RetakesPlugin plugin, GameManager gameManager, RoundEventHandlers roundEventHandlers)
    {
        _plugin = plugin;
        _gameManager = gameManager;
        _roundEventHandlers = roundEventHandlers;
    }

    public void OnCommandForceBombsite(CCSPlayerController? player, CommandInfo commandInfo)
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

    public void OnCommandForceBombsiteStop(CCSPlayerController? player, CommandInfo commandInfo)
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

        _roundEventHandlers.SetForcedBombsite(null);

        commandInfo.ReplyToCommand($"{_plugin.Localizer["retakes.prefix"]} The bombsite will no longer be forced.");
        Logger.LogInfo("Commands", $"Forced bombsite cleared by {player?.PlayerName ?? "Console"}");
    }

    public void OnCommandScramble(CCSPlayerController? player, CommandInfo commandInfo)
    {
        if (player != null && !PlayerHelper.IsValid(player))
        {
            return;
        }

        if (!AdminManager.PlayerHasPermissions(player, "@css/admin"))
        {
            commandInfo.ReplyToCommand($"{_plugin.Localizer["retakes.prefix"]} {_plugin.Localizer["retakes.no_permissions"]}");
            return;
        }

        _gameManager.ScrambleNextRound(player);
        Logger.LogInfo("Commands", $"Teams scramble requested by {player?.PlayerName ?? "Console"}");
    }

    public void OnCommandDebugState(CCSPlayerController? player, CommandInfo commandInfo)
    {
        if (player != null)
        {
            commandInfo.ReplyToCommand($"{_plugin.Localizer["retakes.prefix"]} This command can only be executed from the server console.");
            return;
        }

        if (!AdminManager.PlayerHasPermissions(player, "@css/root"))
        {
            commandInfo.ReplyToCommand($"{_plugin.Localizer["retakes.prefix"]} {_plugin.Localizer["retakes.no_permissions"]}");
            return;
        }

        _gameManager.QueueManager.DebugQueues(true);
    }
}