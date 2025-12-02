using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;

using RetakesPlugin.Utils;

namespace RetakesPlugin.Commands.SpawnEditor;

public class HideSpawnsCommand
{
    private readonly RetakesPlugin _plugin;
    private readonly ShowSpawnsCommand _showSpawnsCommand;

    public HideSpawnsCommand(RetakesPlugin plugin, ShowSpawnsCommand showSpawnsCommand)
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

        _showSpawnsCommand.SetShowingSpawnsForBombsite(null);

        Server.ExecuteCommand("mp_warmup_pausetimer 0");
        Server.ExecuteCommand("mp_warmup_end");

        Logger.LogInfo("Commands", $"{player?.PlayerName} exited spawn editing mode");
    }
}