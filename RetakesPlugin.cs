using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes;
using RetakesPlugin.Modules.Config;

namespace RetakesPlugin;

[MinimumApiVersion(129)]
public class RetakesPlugin : BasePlugin
{
    public override string ModuleName => "Retakes Plugin";
    public override string ModuleVersion => "0.0.1";
    public override string ModuleAuthor => "B3none";
    public override string ModuleDescription => "Community retakes for CS2.";

    public const string MessagePrefix = "[Retakes] ";
    private MapConfig? _mapConfig;
    
    public override void Load(bool hotReload)
    {
        Console.WriteLine(MessagePrefix + "Plugin loaded!");
        
        _mapConfig = new MapConfig(ModuleDirectory, Server.MapName);
        _mapConfig.Load();
        
        throw new Exception($"No config for map {Server.MapName}!");
    }
}