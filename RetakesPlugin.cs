using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using RetakesPlugin.Modules.Config;

namespace RetakesPlugin;

[MinimumApiVersion(129)]
public class RetakesPlugin : BasePlugin
{
    public override string ModuleName => "Retakes Plugin";
    public override string ModuleVersion => "0.0.1";
    public override string ModuleAuthor => "B3none";
    public override string ModuleDescription => "Community retakes for CS2.";

    // TODO: Add colours for message prefix.
    public const string MessagePrefix = "[Retakes] ";
    
    private MapConfig? _mapConfig;
    
    public override void Load(bool hotReload)
    {
        Console.WriteLine(MessagePrefix + "Plugin loaded!");
        
        RegisterListener<Listeners.OnMapStart>(OnMapStartHandler);

        if (hotReload)
        {
            OnMapStartHandler(Server.MapName);
        }
    }
    
    private void OnMapStartHandler(string mapName)
    {
        // If we don't have a map config loaded, load it.
        if (_mapConfig == null || _mapConfig.MapName != Server.MapName)
        {
            _mapConfig = new MapConfig(ModuleDirectory, Server.MapName);
            _mapConfig.Load();
        }
    }
    
    [GameEventHandler]
    public HookResult OnRoundPreStart(EventRoundPrestart @event, GameEventInfo info)
    {
        Console.WriteLine(MessagePrefix + "Round Pre Start event fired!");
        
        return HookResult.Continue;
    }
}