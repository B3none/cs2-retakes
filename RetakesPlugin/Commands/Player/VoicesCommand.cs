using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Utils;

using RetakesPlugin.Configs;
using RetakesPlugin.Utils;

namespace RetakesPlugin.Commands.Player;

public class VoicesCommand
{
    private readonly HashSet<CCSPlayerController> _hasMutedVoices;
    private readonly BaseConfigs _config;
    private readonly RetakesPlugin _plugin;

    public VoicesCommand(RetakesPlugin plugin, BaseConfigs config, HashSet<CCSPlayerController> hasMutedVoices)
    {
        _plugin = plugin;
        _config = config;
        _hasMutedVoices = hasMutedVoices;
    }

    public void OnCommand(CCSPlayerController? player, CommandInfo commandInfo)
    {
        if (!PlayerHelper.IsValid(player))
        {
            commandInfo.ReplyToCommand($"{_plugin.Localizer["retakes.prefix"]} You must be a valid player to use this command.");
            return;
        }

        if (!_config.MapConfig.EnableBombsiteAnnouncementVoices)
        {
            commandInfo.ReplyToCommand($"{_plugin.Localizer["retakes.prefix"]} Bombsite voice announcements are permanently disabled on this server.");
            return;
        }

        var didMute = false;
        if (!_hasMutedVoices.Contains(player))
        {
            didMute = true;
            _hasMutedVoices.Add(player);
        }
        else
        {
            _hasMutedVoices.Remove(player);
        }

        var statusText = didMute ? $"{_plugin.Localizer["retakes.disabled"]}" : $"{_plugin.Localizer["retakes.enabled"]}";

        commandInfo.ReplyToCommand($"{_plugin.Localizer["retakes.prefix"]} {_plugin.Localizer["retakes.voices.toggle", statusText]}");

        Logger.LogInfo("Commands", $"{player.PlayerName} {(didMute ? "muted" : "unmuted")} voice announcements");
    }
}