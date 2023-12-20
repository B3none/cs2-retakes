using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Utils;
using RetakesPlugin.Modules;
using RetakesPlugin.Modules.Config;
using Helpers = RetakesPlugin.Modules.Helpers;

namespace RetakesPlugin;

[MinimumApiVersion(129)]
public class RetakesPlugin : BasePlugin
{
    public override string ModuleName => "Retakes Plugin";
    public override string ModuleVersion => "0.0.1";
    public override string ModuleAuthor => "B3none";
    public override string ModuleDescription => "Community retakes for CS2.";

    // Constants
    // TODO: Add colours for message prefix.
    public const string MessagePrefix = "[Retakes] ";
    
    // Config
    private MapConfig? _mapConfig;
    
    // State
    static CCSGameRules? _gameRules;
    private Bombsite _currentBombsite = Bombsite.A;
    private List<CCSPlayerController> _players = new();
    private Random _random = new();
    
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
    [RequiresPermissions("@css/root")]
    public void AddSpawnCommand(CCSPlayerController? player, CommandInfo commandInfo)
    {
        if (!Helpers.CanPlayerAddSpawn(player))
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

        // If we don't have the game rules, get them.
        _gameRules ??= Helpers.GetGameRules();
        
        if (_gameRules == null)
        {
            Console.WriteLine($"{MessagePrefix}Game rules not found.");
            return HookResult.Continue;
        }
        
        // If we are in warmup, skip.
        if (_gameRules is { WarmupPeriod: true })
        {
            Console.WriteLine($"{MessagePrefix}Warmup round, skipping.");
            return HookResult.Continue;
        }
        
        // Randomly set the bombsite for the current round.
        _currentBombsite = _random.Next(0, 2) == 0 ? Bombsite.A : Bombsite.B;
        
        // TODO: Cache the spawns so we don't have to do this every round.
        // Filter the spawns.
        List<Spawn> tSpawns = new();
        List<Spawn> ctSpawns = new();
        foreach (var spawn in _mapConfig!.GetSpawnsClone())
        {
            if (spawn.Bombsite != _currentBombsite)
            {
                continue;
            }
            
            switch (spawn.Team)
            {
                case CsTeam.Terrorist:
                    tSpawns.Add(spawn);
                    break;
                case CsTeam.CounterTerrorist:
                    ctSpawns.Add(spawn);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        
        Console.WriteLine($"{MessagePrefix}There are {tSpawns.Count} Terrorist, and {ctSpawns.Count} Counter-Terrorist spawns available for bombsite {(_currentBombsite == Bombsite.A ? "A" : "B")}.");

        // Now move the players to their spawns.
        foreach (var player in Utilities.GetPlayers())
        {
            if (!Helpers.IsValidPlayer(player) || player.TeamNum < (int)CsTeam.Terrorist)
            {
                continue;
            }
            
            var playerPawn = player.PlayerPawn.Value;

            if (playerPawn == null)
            {
                continue;
            }
            
            var isTerrorist = player.TeamNum == (int)CsTeam.Terrorist;

            var spawn = Helpers.GetAndRemoveRandomItem(isTerrorist ? tSpawns : ctSpawns);
            
            playerPawn.Teleport(spawn.Vector, spawn.QAngle, new Vector());
        }
        
        return HookResult.Continue;
    }
}
