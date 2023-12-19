using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes;

namespace RetakesPlugin;

[MinimumApiVersion(129)]
public class RetakesPlugin : BasePlugin
{
    public override string ModuleName => "Retakes Plugin";
    public override string ModuleVersion => "0.0.1";
    public override string ModuleAuthor => "B3none";
    public override string ModuleDescription => "Community retakes gamemode for CS2.";
    
    public override void Load(bool hotReload)
    {
        Console.WriteLine("Retakes loaded!");
    }
}