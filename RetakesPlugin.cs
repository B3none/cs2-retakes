using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Utils;
using RetakesPlugin.Modules;
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
        Console.WriteLine($"{MessagePrefix}Plugin loaded!");
        
        RegisterListener<Listeners.OnMapStart>(OnMapStartHandler);

        if (hotReload)
        {
            OnMapStartHandler(Server.MapName);
        }
    }
    
    // Commands
    
    [ConsoleCommand("css_addspawn", "Adds a spawn point for retakes to the map.")]
    [CommandHelper(minArgs: 2, usage: "[T/CT] [A/B] [Y/N (can be planter / optional)]", whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void AddSpawnCommand(CCSPlayerController? player, CommandInfo commandInfo)
    {
        if (!CanPlayerAddSpawn(player))
        {
            commandInfo.ReplyToCommand($"{MessagePrefix}You must be a player.");
            return;
        }
        
        var team = commandInfo.GetArg(1).ToUpper();
        if (team != "T" && team != "CT")
        {
            commandInfo.ReplyToCommand($"{MessagePrefix}You must specify a team [T / CT].");
            return;
        }
        
        var bombsite = commandInfo.GetArg(2).ToUpper();
        if (bombsite != "A" && team != "B")
        {
            commandInfo.ReplyToCommand($"{MessagePrefix}You must specify a bombsite [A / B].");
            return;
        }

        var canBePlanter = commandInfo.GetArg(3).ToUpper();
        if (canBePlanter != "" && canBePlanter != "Y" && canBePlanter != "N")
        {
            commandInfo.ReplyToCommand($"{MessagePrefix}Invalid value passed to can be a planter [Y / N].");
            return;
        }

        var spawn = new Spawn(
            vector: player!.PlayerPawn.Value!.AbsOrigin!,
            qAngle: player!.PlayerPawn.Value!.AbsRotation!
        )
        {
            Team = team == "T" ? CsTeam.Terrorist : CsTeam.CounterTerrorist,
            CanBePlanter = team == "T" && canBePlanter == "Y",
            Bombsite = bombsite == "A" ? Bombsite.A : Bombsite.B
        };

        if (_mapConfig == null)
        {
            commandInfo.ReplyToCommand($"{MessagePrefix}Map config not loaded for some reason...");
            return;
        }
        
        _mapConfig.AddSpawn(spawn);
        
        commandInfo.ReplyToCommand($"{MessagePrefix}Adding spawn.");
    }
    
    // Listeners
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
        Console.WriteLine($"{MessagePrefix}Round Pre Start event fired!");
        
        return HookResult.Continue;
    }
    
    // Helpers
    private static bool IsValidPlayer(CCSPlayerController? player)
    {
        return player != null && player.IsValid;
    }
    
    private static bool CanPlayerAddSpawn(CCSPlayerController? player)
    {
        if (!IsValidPlayer(player))
        {
            return false;
        }
        
        var playerPawn = player!.PlayerPawn.Value;
        
        return playerPawn != null
               && playerPawn is { Health: > 0, AbsOrigin: not null, AbsRotation: not null };
    }
}