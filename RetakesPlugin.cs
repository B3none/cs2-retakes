using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
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
    private const string Version = "1.0.5";
    
    public override string ModuleName => "Retakes Plugin";
    public override string ModuleVersion => Version;
    public override string ModuleAuthor => "B3none";
    public override string ModuleDescription => "Community retakes for CS2.";

    // Constants
    public static readonly string LogPrefix = $"[Retakes {Version}] ";
    public static readonly string MessagePrefix = $"[{ChatColors.Green}Retakes{ChatColors.White}] ";
    
    // Config
    private MapConfig? _mapConfig;
    
    // State
    private static CCSGameRules? _gameRules;
    private Bombsite _currentBombsite = Bombsite.A;
    private readonly Game _gameManager = new();
    private CCSPlayerController? _planter;
    private readonly Random _random = new();
    private bool _didTerroristsWinLastRound;

    public override void Load(bool hotReload)
    {
        Console.WriteLine($"{LogPrefix}Plugin loaded!");
        
        RegisterListener<Listeners.OnMapStart>(OnMapStart);

        if (hotReload)
        {
            // If a hot reload is detected restart the current map.
            Server.ExecuteCommand($"map {Server.MapName}");
        }
    }
    
    // Commands
    [ConsoleCommand("css_addspawn", "Adds a spawn point for retakes to the map.")]
    [CommandHelper(minArgs: 2, usage: "[T/CT] [A/B] [Y/N (can be planter / default N)]", whoCanExecute: CommandUsage.CLIENT_ONLY)]
    [RequiresPermissions("@css/root")]
    public void AddSpawnCommand(CCSPlayerController? player, CommandInfo commandInfo)
    {
        if (!Helpers.DoesPlayerHavePawn(player))
        {
            commandInfo.ReplyToCommand($"{LogPrefix}You must be a player.");
            return;
        }
        
        var team = commandInfo.GetArg(1).ToUpper();
        if (team != "T" && team != "CT")
        {
            commandInfo.ReplyToCommand($"{LogPrefix}You must specify a team [T / CT] - [Value: {team}].");
            return;
        }
        
        var bombsite = commandInfo.GetArg(2).ToUpper();
        if (bombsite != "A" && bombsite != "B")
        {
            commandInfo.ReplyToCommand($"{LogPrefix}You must specify a bombsite [A / B] - [Value: {bombsite}].");
            return;
        }

        var canBePlanter = commandInfo.GetArg(3).ToUpper();
        if (canBePlanter != "" && canBePlanter != "Y" && canBePlanter != "N")
        {
            commandInfo.ReplyToCommand($"{LogPrefix}Invalid value passed to can be a planter [Y / N] - [Value: {canBePlanter}].");
            return;
        }

        if (team != "T" && canBePlanter == "Y")
        {
            commandInfo.ReplyToCommand($"{LogPrefix}It looks like you tried to place a bomb planter spawn for a CT? Is this correct?");
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
            commandInfo.ReplyToCommand($"{LogPrefix}Map config not loaded for some reason...");
            return;
        }
        
        var didAddSpawn = _mapConfig.AddSpawn(spawn);
        
        commandInfo.ReplyToCommand($"{LogPrefix}{(didAddSpawn ? "Spawn added" : "Error adding spawn")}");
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

    [ConsoleCommand("css_balance", "Manually trigger team balance")]
    [RequiresPermissions("@css/root")]
    public void OnCommandBalance(CCSPlayerController? player, CommandInfo command)
    {
        if (!Helpers.IsValidPlayer(player))
        {
            return;
        }
        
        _gameManager.BalanceTeams();
    }

    // Listeners
    private void OnMapStart(string mapName)
    {
        Console.WriteLine($"{LogPrefix}OnMapStart listener triggered!");
        
        // Execute the retakes configuration.
        Helpers.ExecuteRetakesConfiguration();
        
        // If we don't have a map config loaded, load it.
        if (_mapConfig == null || _mapConfig.MapName != Server.MapName)
        {
            _mapConfig = new MapConfig(ModuleDirectory, Server.MapName);
            _mapConfig.Load();
        }
    }

    [GameEventHandler]
    public HookResult OnPlayerConnectFull(EventPlayerConnectFull @event, GameEventInfo info)
    {
        var player = @event.Userid;

        if (!Helpers.IsValidPlayer(player))
        {
            return HookResult.Continue;
        }
        
        player.TeamNum = (int)CsTeam.Spectator;
        player.ForceTeamTime = 3600.0f;

        return HookResult.Continue;
    }
    
    [GameEventHandler]
    public HookResult OnRoundPreStart(EventRoundPrestart @event, GameEventInfo info)
    {
        Console.WriteLine($"{LogPrefix}Round Pre Start event fired!");

        // If we don't have the game rules, get them.
        _gameRules = Helpers.GetGameRules();
        
        if (_gameRules == null)
        {
            Console.WriteLine($"{LogPrefix}Game rules not found.");
            return HookResult.Continue;
        }
        
        // If we are in warmup, skip.
        if (_gameRules.WarmupPeriod)
        {
            Console.WriteLine($"{LogPrefix}Warmup round, skipping.");
            return HookResult.Continue;
        }
        
        if (!_gameManager.Queue.ActivePlayers.Any())
        {
            Console.WriteLine($"{LogPrefix}No active players, skipping.");
            _gameManager.SetupActivePlayers();
            return HookResult.Continue;
        }
        
        // Update Queue status
        _gameManager.Queue.DebugQueues(true);
        _gameManager.Queue.Update();
        _gameManager.Queue.DebugQueues(false);
        
        // Handle team swaps at the start of the round
        if (!_didTerroristsWinLastRound)
        {
            _gameManager.CounterTerroristRoundWin();

            return HookResult.Continue;
        }
        
        _gameManager.TerroristRoundWin();

        return HookResult.Continue;
    }

    [GameEventHandler]
    public HookResult OnRoundStart(EventRoundStart @event, GameEventInfo info)
    {
        Console.WriteLine($"{LogPrefix}Round Start event fired!");

        // If we don't have the game rules, get them.
        _gameRules = Helpers.GetGameRules();
        
        if (_gameRules == null)
        {
            Console.WriteLine($"{LogPrefix}Game rules not found.");
            return HookResult.Continue;
        }
        
        // If we are in warmup, skip.
        if (_gameRules.WarmupPeriod)
        {
            Console.WriteLine($"{LogPrefix}Warmup round, skipping.");
            return HookResult.Continue;
        }
        
        // Reset round state.
        _currentBombsite = _random.Next(0, 2) == 0 ? Bombsite.A : Bombsite.B;
        _planter = null;
        _gameManager.ResetPlayerScores();
        
        // TODO: Cache the spawns so we don't have to do this every round.
        // TODO: Move spawning functionality to a "SpawnManager"
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
        
        Console.WriteLine($"{LogPrefix}There are {tSpawns.Count} Terrorist, and {ctSpawns.Count} Counter-Terrorist spawns available for bombsite {(_currentBombsite == Bombsite.A ? "A" : "B")}.");
        // Server.PrintToChatAll($"{MessagePrefix}There are {tSpawns.Count} Terrorist, and {ctSpawns.Count} Counter-Terrorist spawns available for bombsite {(_currentBombsite == Bombsite.A ? "A" : "B")}.");
        
        Console.WriteLine($"{LogPrefix}Moving players to spawns.");
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

                if (spawnIndex == -1)
                {
                    Console.WriteLine($"{LogPrefix}No bomb planter spawn found in configuration.");
                    throw new Exception("No bomb planter spawn found in configuration.");
                }
                
                spawn = tSpawns[spawnIndex];
                
                tSpawns.RemoveAt(spawnIndex);
            }
            else
            {
                Console.WriteLine($"{LogPrefix}GetAndRemoveRandomItem called.");
                spawn = Helpers.GetAndRemoveRandomItem(isTerrorist ? tSpawns : ctSpawns);
                Console.WriteLine($"{LogPrefix}GetAndRemoveRandomItem complete.");
            }
            
            playerPawn.Teleport(spawn.Vector, spawn.QAngle, new Vector());
        }
        Console.WriteLine($"{LogPrefix}Moving players to spawns COMPLETE.");
        
        Console.WriteLine($"{LogPrefix}Printing bombsite output to all players.");
        Server.PrintToChatAll($"{MessagePrefix}Bombsite: {(_currentBombsite == Bombsite.A ? "A" : "B")}");
        Console.WriteLine($"{LogPrefix}Printing bombsite output to all players COMPLETE.");
        
        return HookResult.Continue;
    }
    
    [GameEventHandler]
    public HookResult OnRoundPostStart(EventRoundPoststart @event, GameEventInfo info)
    {
        Console.WriteLine($"{LogPrefix}OnRoundPostStart event fired.");
        
        foreach (var player in _gameManager.Queue.ActivePlayers)
        {
            if (!Helpers.IsValidPlayer(player))
            {
                continue;
            }
                
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

        return HookResult.Continue;
    }
    
    [GameEventHandler]
    public HookResult OnPlayerSpawn(EventPlayerSpawn @event, GameEventInfo info)
    {
        Console.WriteLine($"{LogPrefix}OnPlayerSpawn event fired.");
        
        // If we don't have the game rules, get them.
        _gameRules = Helpers.GetGameRules();
        
        if (_gameRules == null)
        {
            Console.WriteLine($"{LogPrefix}Game rules not found.");
            return HookResult.Continue;
        }
        
        // If we are in warmup, skip.
        if (_gameRules.WarmupPeriod)
        {
            Console.WriteLine($"{LogPrefix}Warmup round, balance teams.");
            _gameManager.BalanceTeams();
            return HookResult.Continue;
        }
        
        var player = @event.Userid;

        if (!Helpers.IsValidPlayer(player) || !Helpers.IsPlayerConnected(player))
        {
            return HookResult.Continue;
        }
        
        // debug and check if the player is in the queue.
        Console.WriteLine($"{LogPrefix}[{player.PlayerName}] Checking ActivePlayers.");
        if (!_gameManager.Queue.ActivePlayers.Contains(player))
        {
            Console.WriteLine($"{LogPrefix}[{player.PlayerName}] Player not in ActivePlayers, moving to spectator.");
            if (!player.IsBot)
            {
                Console.WriteLine($"{LogPrefix}[{player.PlayerName}] moving to spectator.");
                player.ChangeTeam(CsTeam.Spectator);
            }
            
            Console.WriteLine($"{LogPrefix}[{player.PlayerName}] Checking player pawn {player.PlayerPawn.Value != null}.");
            if (player.PlayerPawn.Value != null && player.PlayerPawn.IsValid && player.PlayerPawn.Value.IsValid)
            {
                Console.WriteLine($"{LogPrefix}[{player.PlayerName}] Checking player pawn is valid {player.PlayerPawn.IsValid} && {player.PlayerPawn.Value.IsValid}.");
                Console.WriteLine($"{LogPrefix}[{player.PlayerName}] calling playerpawn.commitsuicide()");
                player.PlayerPawn.Value.CommitSuicide(false, true);
                Console.WriteLine($"{LogPrefix}[{player.PlayerName}] removing pawn entity.");
                player.PlayerPawn.Value.Remove();
            }
            return HookResult.Continue;
        }
        else
        {
            Console.WriteLine($"{LogPrefix}[{player.PlayerName}] Player is in ActivePlayers.");
        }

        return HookResult.Continue;
    }
    
    [GameEventHandler]
    public HookResult OnWeaponFire(EventWeaponFire @event, GameEventInfo info)
    {
        var player = @event.Userid;

        if (!Helpers.IsValidPlayer(player))
        {
            return HookResult.Continue;
        }

        if (Helpers.HasBomb(player))
        {
            Console.WriteLine($"{LogPrefix}Player has bomb, swap to bomb userid({(int)player.UserId!}).");
            
            // TODO: Investigate this because sometimes it doesn't work.
            // Change to their knife to prevent planting.
            NativeAPI.IssueClientCommand((int)player.UserId!, "slot5");
        }

        return HookResult.Continue;
    }

    [GameEventHandler]
    public HookResult OnBombDropped(EventBombDropped @event, GameEventInfo info)
    {
        var player = @event.Userid;
        
        if (!Helpers.IsValidPlayer(player))
        {
            return HookResult.Continue;
        }
        
        // Remove the bomb entity and give the player that dropped it the bomb
        var bombEntities = Utilities.FindAllEntitiesByDesignerName<CBaseEntity>("weapon_c4").ToList();

        if (bombEntities.Any())
        {
            foreach (var bomb in bombEntities)
            {
                bomb.Remove();
            }
        }
        
        Helpers.GiveAndSwitchToBomb(player);
        
        return HookResult.Continue;
    }

    [GameEventHandler]
    public HookResult OnPlayerDeath(EventPlayerDeath @event, GameEventInfo info)
    {
        var attacker = @event.Attacker;
        var assister = @event.Assister;

        if (!Helpers.IsValidPlayer(attacker))
        {
            _gameManager.AddScore(attacker, Game.ScoreForKill);
        }

        if (!Helpers.IsValidPlayer(assister))
        {
            _gameManager.AddScore(assister, Game.ScoreForKill);
        }

        return HookResult.Continue;
    }
    
    [GameEventHandler]
    public HookResult OnBombDefused(EventBombDefused @event, GameEventInfo info)
    {
        var player = @event.Userid;

        if (!Helpers.IsValidPlayer(player))
        {
            return HookResult.Continue;
        }
        
        _gameManager.AddScore(player, Game.ScoreForDefuse);

        return HookResult.Continue;
    }
    
    [GameEventHandler]
    public HookResult OnRoundEnd(EventRoundEnd @event, GameEventInfo info)
    {
        Console.WriteLine($"{LogPrefix}OnRoundEnd event fired.");

        _didTerroristsWinLastRound = @event.Winner == (int)CsTeam.Terrorist;

        return HookResult.Continue;
    }
    
    [GameEventHandler]
    public HookResult OnPlayerTeam(EventPlayerTeam @event, GameEventInfo info)
    {
        Console.WriteLine($"{LogPrefix}OnRoundEnd event fired.");
        
        var player = @event.Userid;

        if (!Helpers.IsValidPlayer(player))
        {
            return HookResult.Continue;
        }
        
        Console.WriteLine($"{LogPrefix}[{player.PlayerName}] OnPlayerTeam event fired. ({(@event.Isbot ? "BOT" : "NOT BOT")}) {(CsTeam)@event.Oldteam} -> {(CsTeam)@event.Team}");
        
        // If we don't have the game rules, get them.
        _gameRules = Helpers.GetGameRules();
        
        if (_gameRules == null)
        {
            Console.WriteLine($"{LogPrefix}Game rules not found.");
            return HookResult.Continue;
        }
        
        _gameManager.Queue.DebugQueues(true);
        _gameManager.Queue.PlayerTriedToJoinTeam(player, @event.Team != (int)CsTeam.Spectator, _gameRules.WarmupPeriod);
        _gameManager.Queue.DebugQueues(false);

        return HookResult.Continue;
    }
    
    [GameEventHandler]
    public HookResult OnPlayerDisconnect(EventPlayerDisconnect @event, GameEventInfo info)
    {
        Console.WriteLine($"{LogPrefix}OnPlayerDisconnect event fired.");
        var player = @event.Userid;
        
        if (!Helpers.IsValidPlayer(player))
        {
            return HookResult.Continue;
        }
        
        _gameManager.Queue.DebugQueues(true);
        _gameManager.Queue.PlayerDisconnected(player);
        _gameManager.Queue.DebugQueues(false);

        return HookResult.Continue;
    }
}
