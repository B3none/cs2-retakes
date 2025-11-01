using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Core.Attributes.Registration;

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

    [ConsoleCommand("css_forcebombsite", "Force the retakes to occur from a single bombsite.")]
    [CommandHelper(minArgs: 1, usage: "[A/B]", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    [RequiresPermissions("@css/root")]
    public void OnCommandForceBombsite(CCSPlayerController? player, CommandInfo commandInfo)
    {
        if (player != null && !PlayerHelper.IsValid(player))
        {
            return;
        }

        var bombsite = commandInfo.GetArg(1).ToUpper();
        if (bombsite != "A" && bombsite != "B")
        {
            commandInfo.ReplyToCommand($"{_plugin.Localizer["retakes.prefix"]}  You must specify a bombsite [A / B].");
            return;
        }

        var forcedBombsite = bombsite == "A" ? Bombsite.A : Bombsite.B;
        _roundEventHandlers.SetForcedBombsite(forcedBombsite);

        commandInfo.ReplyToCommand($"{_plugin.Localizer["retakes.prefix"]}  The bombsite will now be forced to {forcedBombsite}.");
        Logger.LogInfo("Commands", $"Bombsite forced to {forcedBombsite} by {player?.PlayerName ?? "Console"}");
    }

    [ConsoleCommand("css_forcebombsitestop", "Clear the forced bombsite and return back to normal.")]
    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    [RequiresPermissions("@css/root")]
    public void OnCommandForceBombsiteStop(CCSPlayerController? player, CommandInfo commandInfo)
    {
        if (player != null && !PlayerHelper.IsValid(player))
        {
            return;
        }

        _roundEventHandlers.SetForcedBombsite(null);

        commandInfo.ReplyToCommand($"{_plugin.Localizer["retakes.prefix"]} The bombsite will no longer be forced.");
        Logger.LogInfo("Commands", $"Forced bombsite cleared by {player?.PlayerName ?? "Console"}");
    }

    [ConsoleCommand("css_scramble", "Sets teams to scramble on the next round.")]
    [ConsoleCommand("css_scrambleteams", "Sets teams to scramble on the next round.")]
    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    [RequiresPermissions("@css/admin")]
    public void OnCommandScramble(CCSPlayerController? player, CommandInfo commandInfo)
    {
        _gameManager.ScrambleNextRound(player);
        Logger.LogInfo("Commands", $"Teams scramble requested by {player?.PlayerName ?? "Console"}");
    }

    [ConsoleCommand("css_debugqueues", "Prints the state of the queues to the console.")]
    [CommandHelper(whoCanExecute: CommandUsage.SERVER_ONLY)]
    [RequiresPermissions("@css/root")]
    public void OnCommandDebugState(CCSPlayerController? player, CommandInfo commandInfo)
    {
        _gameManager.QueueManager.DebugQueues(true);
    }
}