using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;

using RetakesPlugin.Utils;

namespace RetakesPlugin.Commands.MapConfig;

public class MapConfigsCommand
{
    private readonly RetakesPlugin _plugin;
    private readonly string _moduleDirectory;

    public MapConfigsCommand(RetakesPlugin plugin, string moduleDirectory)
    {
        _plugin = plugin;
        _moduleDirectory = moduleDirectory;
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

        var mapConfigDirectory = Path.Combine(_moduleDirectory, "map_config");

        if (!Directory.Exists(mapConfigDirectory))
        {
            commandInfo.ReplyToCommand($"{_plugin.Localizer["retakes.prefix"]} No map configs found.");
            return;
        }

        var files = Directory.GetFiles(mapConfigDirectory);
        Array.Sort(files);

        if (files.Length == 0)
        {
            commandInfo.ReplyToCommand($"{_plugin.Localizer["retakes.prefix"]} No map configs found.");
            return;
        }

        foreach (var file in files)
        {
            var transformedFile = file
                .Replace($"{mapConfigDirectory}/", "")
                .Replace(".json", "");

            commandInfo.ReplyToCommand($"{_plugin.Localizer["retakes.prefix"]} !mapconfig {transformedFile}");
            player?.PrintToConsole($"{_plugin.Localizer["retakes.prefix"]} !mapconfig {transformedFile}");
        }

        commandInfo.ReplyToCommand($"{_plugin.Localizer["retakes.prefix"]} A list of available map configs has been outputted above.");
    }
}