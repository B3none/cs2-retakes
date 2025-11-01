using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Core.Attributes.Registration;

using RetakesPlugin.Utils;

namespace RetakesPlugin.Commands;

public class MapConfigCommands
{
    private readonly RetakesPlugin _plugin;
    private readonly string _moduleDirectory;
    private Action<string> _onMapConfigLoad;

    public MapConfigCommands(RetakesPlugin plugin, string moduleDirectory, Action<string> onMapConfigLoad)
    {
        _plugin = plugin;
        _moduleDirectory = moduleDirectory;
        _onMapConfigLoad = onMapConfigLoad;
    }

    [ConsoleCommand("css_mapconfig", "Forces a specific map config file to load.")]
    [ConsoleCommand("css_setmapconfig", "Forces a specific map config file to load.")]
    [ConsoleCommand("css_loadmapconfig", "Forces a specific map config file to load.")]
    [CommandHelper(minArgs: 1, usage: "[filename]", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    [RequiresPermissions("@css/root")]
    public void OnCommandMapConfig(CCSPlayerController? player, CommandInfo commandInfo)
    {
        if (player != null && !PlayerHelper.IsValid(player))
        {
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

    [ConsoleCommand("css_mapconfigs", "Displays a list of available map configs.")]
    [ConsoleCommand("css_viewmapconfigs", "Displays a list of available map configs.")]
    [ConsoleCommand("css_listmapconfigs", "Displays a list of available map configs.")]
    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    [RequiresPermissions("@css/root")]
    public void OnCommandMapConfigs(CCSPlayerController? player, CommandInfo commandInfo)
    {
        if (player != null && !PlayerHelper.IsValid(player))
        {
            return;
        }

        var mapConfigDirectory = Path.Combine(_moduleDirectory, "map_config");
        var files = Directory.GetFiles(mapConfigDirectory);

        Array.Sort(files);

        if (!Directory.Exists(mapConfigDirectory) || files.Length == 0)
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