using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;

using RetakesPlugin.Services;
using RetakesPlugin.Utils;
using RetakesPluginShared.Enums;

namespace RetakesPlugin.Commands.SpawnEditor;

public class ShowSpawnsCommand
{
    private readonly RetakesPlugin _plugin;
    private readonly MapConfigService _mapConfigService;
    private Bombsite? _showingSpawnsForBombsite;

    public ShowSpawnsCommand(RetakesPlugin plugin, MapConfigService mapConfigService)
    {
        _plugin = plugin;
        _mapConfigService = mapConfigService;
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

        if (!AdminManager.PlayerHasPermissions(player, "@css/root"))
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
            if (_showingSpawnsForBombsite != null)
            {
                SpawnService.ShowSpawns(_plugin, _mapConfigService.GetSpawnsClone(), _showingSpawnsForBombsite);
                Logger.LogInfo("Commands", $"Spawns displayed for bombsite {_showingSpawnsForBombsite}");
            }
        });

        Logger.LogInfo("Commands", $"Showing spawns for bombsite {_showingSpawnsForBombsite} to {player!.PlayerName}");
    }
}