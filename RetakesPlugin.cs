using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Utils;
using RetakesPlugin.Modules;
using RetakesPlugin.Modules.Allocators;
using RetakesPlugin.Modules.Config;
using RetakesPlugin.Modules.Managers;
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
    private static CCSGameRules? _gameRules;
    private Bombsite _currentBombsite = Bombsite.A;
    private Game _gameManager = new();
    private CCSPlayerController? _planter;
    private readonly Random _random = new();

    public override void Load(bool hotReload)
    {
        Console.WriteLine($"{MessagePrefix}Plugin loaded!");
        
        RegisterListener<Listeners.OnMapStart>(OnMapStartHandler);

        if (hotReload)
        {
            _mapConfig = null;
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
            commandInfo.ReplyToCommand($"{MessagePrefix}You must specify a team [T / CT] - [Value: {team}].");
            return;
        }
        
        var bombsite = commandInfo.GetArg(2).ToUpper();
        if (bombsite != "A" && bombsite != "B")
        {
            commandInfo.ReplyToCommand($"{MessagePrefix}You must specify a bombsite [A / B] - [Value: {bombsite}].");
            return;
        }

        var canBePlanter = commandInfo.GetArg(3).ToUpper();
        if (canBePlanter != "" && canBePlanter != "Y" && canBePlanter != "N")
        {
            commandInfo.ReplyToCommand($"{MessagePrefix}Invalid value passed to can be a planter [Y / N] - [Value: {canBePlanter}].");
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
        
        var didAddSpawn = _mapConfig.AddSpawn(spawn);
        
        commandInfo.ReplyToCommand($"{MessagePrefix}{(didAddSpawn ? "Spawn added" : "Error adding spawn")}");
    }
    
    [ConsoleCommand("css_teleport", "This command teleports the player to the given coordinates")]
    [RequiresPermissions("@css/root")]
    public void OnCommandTeleport(CCSPlayerController? player, CommandInfo command)
    {
        if (player == null)
        {
            return;
        }
        if (!player.PlayerPawn.IsValid)
        {
            return;
        }

        
        if (command.ArgCount != 4)
        {
            return;
        }

        if (!float.TryParse(command.ArgByIndex(1), out float positionX))
        {
            return;
        }

        if (!float.TryParse(command.ArgByIndex(2), out float positionY))
        {
            return;
        }

        if (!float.TryParse(command.ArgByIndex(3), out float positionZ))
        {
            return;
        }

        player?.PlayerPawn?.Value?.Teleport(new Vector(positionX, positionY, positionZ), new QAngle(0f,0f,0f), new Vector(0f, 0f, 0f));
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
    public HookResult OnRoundStart(EventRoundStart @event, GameEventInfo info)
    {
        Console.WriteLine($"{MessagePrefix}Round Start event fired!");

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
        
        // Reset round state.
        _currentBombsite = _random.Next(0, 2) == 0 ? Bombsite.A : Bombsite.B;
        _planter = null;
        _gameManager.ResetPlayerScores();
        
        // TODO: Cache the spawns so we don't have to do this every round.
        // Filter the spawns.
        List<Spawn> tSpawns = new();
        List<Spawn> ctSpawns = new();
        foreach (var spawn in Helpers.Shuffle(_mapConfig!.GetSpawnsClone()))
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

        // Update Queue status
        _gameManager.Queue.DebugQueues(true);
        _gameManager.Queue.Update();
        _gameManager.Queue.DebugQueues(false);
        
        // Now move the players to their spawns.
        // We shuffle this list to ensure that 1 player does not have to plant every round.
        foreach (var player in Helpers.Shuffle(_gameManager.Queue.ActivePlayers))
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
            
            var isTerrorist = player.TeamNum == (byte)CsTeam.Terrorist;

            Spawn spawn;
            
            if (_planter == null && isTerrorist)
            {
                _planter = player;
                
                var spawnIndex = tSpawns.FindIndex(tSpawn => tSpawn.CanBePlanter);
                spawn = tSpawns[spawnIndex];
                
                tSpawns.RemoveAt(spawnIndex);
            }
            else
            {
                spawn = Helpers.GetAndRemoveRandomItem(isTerrorist ? tSpawns : ctSpawns);
            }
            
            playerPawn.Teleport(spawn.Vector, spawn.QAngle, new Vector());
        }
        
        return HookResult.Continue;
    }
    
    [GameEventHandler]
    public HookResult OnRoundPostStart(EventRoundPoststart @event, GameEventInfo info)
    {
        Console.WriteLine($"{MessagePrefix}OnRoundPostStart event fired.");

        foreach (var player in _gameManager.Queue.ActivePlayers)
        {
            // Strip the player of all of their weapons and the bomb before any spawn / allocation occurs.
            // TODO: Figure out why this is crashing the server / undo workaround.
            // player.RemoveWeapons();
            Helpers.RemoveAllItemsAndEntities(player);
            
            // Create a timer to do this as it would occasionally fire too early.
            AddTimer(0.05f, () =>
            {
                Weapons.Allocate(player);
                Equipment.Allocate(player, player == _planter);
                Grenades.Allocate(player);
            });
        }
        
        // TODO: Add auto plant logic here.

        return HookResult.Continue;
    }
    
    [GameEventHandler]
    public HookResult OnBombBeginPlant(EventBombBeginplant @event, GameEventInfo info)
    {
        Console.WriteLine($"{MessagePrefix}BombBeginplant event fired for {@event.Userid.PlayerName} - bombsite: {(@event.Site == (int)Bombsite.A ? "A" : "B")}");

        var player = @event.Userid;
        
        _gameRules = Helpers.GetGameRules();
        
        Console.WriteLine($"{MessagePrefix}FreezePeriod: {(_gameRules!.FreezePeriod ? "yes" : "no")}");
        
        // Don't allow planting during freeze time.
        if (_gameRules!.FreezePeriod)
        {
            player.PrintToChat($"{MessagePrefix}You cannot plant during freeze time.");
            
            // Change to their knife to prevent planting.
            NativeAPI.IssueClientCommand((int)player.UserId!, "slot3");
        }
        
        return HookResult.Continue;
    }
    
    [GameEventHandler]
    public HookResult OnPlayerDeath(EventPlayerDeath @event, GameEventInfo info)
    {
        var attacker = @event.Attacker;
        var assister = @event.Assister;

        if (!Helpers.IsValidPlayer(attacker) || attacker.IsBot)
        {
            return HookResult.Continue;
        }
        
        _gameManager.AddScore(attacker, Game.ScoreForKill);
        _gameManager.AddScore(assister, Game.ScoreForAssist);

        return HookResult.Continue;
    }
    
    [GameEventHandler]
    public HookResult OnBombDefused(EventBombDefused @event, GameEventInfo info)
    {
        var player = @event.Userid;

        if (!Helpers.IsValidPlayer(player) || player.IsBot)
        {
            return HookResult.Continue;
        }
        
        _gameManager.AddScore(player, Game.ScoreForDefuse);

        return HookResult.Continue;
    }
    
    [GameEventHandler]
    public HookResult OnRoundEnd(EventRoundEnd @event, GameEventInfo info)
    {
        Console.WriteLine($"{MessagePrefix}OnRoundEnd event fired.");

        var didTerroristsWin = @event.Winner == (int)CsTeam.Terrorist;
        
        if (didTerroristsWin)
        {
            _gameManager.TerroristRoundWin();
        }
        else
        {
            _gameManager.CounterTerroristRoundWin();
        }

        return HookResult.Continue;
    }
    
    [GameEventHandler]
    public HookResult OnPlayerTeam(EventPlayerTeam @event, GameEventInfo info)
    {
        Console.WriteLine($"{MessagePrefix}[{@event.Userid.PlayerName}] OnPlayerTeam event fired. ({(@event.Isbot ? "BOT" : "NOT BOT")}) {(CsTeam)@event.Oldteam} -> {(CsTeam)@event.Team}");
        
        _gameManager.Queue.DebugQueues(true);
        _gameManager.Queue.PlayerTriedToJoinTeam(@event.Userid, @event.Team != (int)CsTeam.Spectator);
        _gameManager.Queue.DebugQueues(false);

        return HookResult.Continue;
    }
    
    [GameEventHandler]
    public HookResult OnPlayerDisconnect(EventPlayerDisconnect @event, GameEventInfo info)
    {
        Console.WriteLine($"{MessagePrefix}OnPlayerDisconnect event fired.");

        _gameManager.Queue.DebugQueues(true);
        _gameManager.Queue.PlayerDisconnected(@event.Userid);
        _gameManager.Queue.DebugQueues(false);

        return HookResult.Continue;
    }
}
