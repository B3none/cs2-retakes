using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;

using RetakesPlugin.Utils;
using RetakesPlugin.Services;
using RetakesPluginShared.Enums;

namespace RetakesPlugin.Commands.SpawnEditor;

public class ShowSpawnsCommand
{
    private readonly RetakesPlugin _plugin;
    private Bombsite? _showingSpawnsForBombsite;

    public ShowSpawnsCommand(RetakesPlugin plugin)
    {
        _plugin = plugin;
    }

    public Bombsite? ShowingSpawnsForBombsite => _showingSpawnsForBombsite;

    public void SetShowingSpawnsForBombsite(Bombsite? bombsite)
    {
        _showingSpawnsForBombsite = bombsite;
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

        if (commandInfo.ArgCount < 2)
        {
            commandInfo.ReplyToCommand($"{_plugin.Localizer["retakes.prefix"]} Usage: !showspawns [A/B]");
            return;
        }

        var bombsite = commandInfo.GetArg(1).ToUpper();
        if (bombsite != "A" && bombsite != "B")
        {
            commandInfo.ReplyToCommand($"{_plugin.Localizer["retakes.prefix"]} You must specify a bombsite [A / B].");
            return;
        }

        _showingSpawnsForBombsite = bombsite == "A" ? Bombsite.A : Bombsite.B;

        Server.ExecuteCommand("mp_warmup_pausetimer 1");
        Server.ExecuteCommand("mp_warmuptime 999999");
        Server.ExecuteCommand("mp_warmup_start");

        _plugin.AddTimer(1.0f, () =>
        {
            if (_showingSpawnsForBombsite != null && _plugin.MapConfigService != null)
            {
                SpawnService.ShowSpawns(_plugin, _plugin.MapConfigService.GetSpawnsClone(), _showingSpawnsForBombsite);
                Logger.LogInfo("Commands", $"Spawns displayed for bombsite {_showingSpawnsForBombsite}");
            }
        });

        Logger.LogInfo("Commands", $"Showing spawns for bombsite {_showingSpawnsForBombsite} to {player!.PlayerName}");
    }
}