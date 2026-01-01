using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;

using RetakesPlugin.Utils;

namespace RetakesPlugin.Commands.MapConfig;

public class MapConfigCommand
{
    private readonly RetakesPlugin _plugin;
    private readonly string _moduleDirectory;
    private readonly Action<string> _onMapConfigLoad;

    public MapConfigCommand(RetakesPlugin plugin, string moduleDirectory, Action<string> onMapConfigLoad)
    {
        _plugin = plugin;
        _moduleDirectory = moduleDirectory;
        _onMapConfigLoad = onMapConfigLoad;
    }

    public void OnCommand(CCSPlayerController? player, CommandInfo commandInfo)
    {
        if (player != null && !PlayerHelper.IsValid(player))
        {
            return;
        }

        var commandName = commandInfo.GetArg(0);
        var requiredPermission = PlayerHelper.GetCommandPermission(_plugin.Config, commandName, "MapConfig");
        if (!AdminManager.PlayerHasPermissions(player, requiredPermission))
        {
            commandInfo.ReplyToCommand($"{_plugin.Localizer["retakes.prefix"]} {_plugin.Localizer["retakes.no_permissions"]}");
            return;
        }

        if (commandInfo.ArgCount < 2)
        {
            commandInfo.ReplyToCommand($"{_plugin.Localizer["retakes.prefix"]} Usage: !mapconfig [filename]");
            return;
        }

        var mapConfigDirectory = Path.Combine(_moduleDirectory, "map_config");

        if (!Directory.Exists(mapConfigDirectory))
        {
            commandInfo.ReplyToCommand($"{_plugin.Localizer["retakes.prefix"]} No map configs found.");
            return;
        }

        var mapConfigFileName = commandInfo.GetArg(1).Trim().Replace(".json", "");
        var mapConfigFilePath = Path.Combine(mapConfigDirectory, $"{mapConfigFileName}.json");

        if (!File.Exists(mapConfigFilePath))
        {
            commandInfo.ReplyToCommand($"{_plugin.Localizer["retakes.prefix"]} Map config file not found.");
            return;
        }

        _onMapConfigLoad(mapConfigFileName);

        commandInfo.ReplyToCommand($"{_plugin.Localizer["retakes.prefix"]} The new map config has been successfully loaded.");
        Logger.LogInfo("Commands", $"Map config '{mapConfigFileName}' loaded by {player?.PlayerName ?? "Console"}");
    }
}