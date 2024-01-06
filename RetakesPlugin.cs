using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Utils;
using RetakesPlugin.Modules.Enums;
using RetakesPlugin.Modules.Allocators;
using RetakesPlugin.Modules.Configs;
using RetakesPlugin.Modules.Managers;
using Helpers = RetakesPlugin.Modules.Helpers;

namespace RetakesPlugin;

[MinimumApiVersion(131)]
public class RetakesPlugin : BasePlugin
{
    private const string Version = "1.1.7";
    
    public override string ModuleName => "Retakes Plugin";
    public override string ModuleVersion => Version;
    public override string ModuleAuthor => "B3none";
    public override string ModuleDescription => "Community retakes for CS2.";

    // Constants
    public static readonly string LogPrefix = $"[Retakes {Version}] ";
    public static readonly string MessagePrefix = $"[{ChatColors.Green}Retakes{ChatColors.White}] ";
    
    // Configs
    private MapConfig? _mapConfig;
    private RetakesConfig? _retakesConfig;
    
    // State
    private Bombsite _currentBombsite = Bombsite.A;
    private GameManager? _gameManager;
    private CCSPlayerController? _planter;
    private CsTeam _lastRoundWinner;

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

    [ConsoleCommand("css_showspawns", "This command shows the spawns")]
    [CommandHelper(minArgs: 1, usage: "[A/B]", whoCanExecute: CommandUsage.CLIENT_ONLY)]
    [RequiresPermissions("@css/root")]
    public void OnCommandShowSpawns(CCSPlayerController? player, CommandInfo commandInfo)
    {
        if (!Helpers.IsValidPlayer(player))
        {
            return;
        }
        
        var bombsite = commandInfo.GetArg(1).ToUpper();
        if (bombsite != "A" && bombsite != "B")
        {
            commandInfo.ReplyToCommand($"{LogPrefix}You must specify a bombsite [A / B].");
            return;
        }
        
        if (_mapConfig == null)
        {
            commandInfo.ReplyToCommand($"{LogPrefix}Map config not loaded for some reason...");
            return;
        }
        
        var spawns = _mapConfig.GetSpawnsClone().Where(spawn => spawn.Bombsite == (bombsite == "A" ? Bombsite.A : Bombsite.B)).ToList();
        
        if (spawns.Count == 0)
        {
            commandInfo.ReplyToCommand($"{LogPrefix}No spawns found for bombsite {bombsite}.");
            return;
        }
        
        // Pre cache the sprites.
        Server.PrecacheModel("sprites/laserbeam.vmt");
        Server.PrecacheModel("sprites/halo.vmt");
        
        // TODO: Display the sprites to show each spawn.
        foreach (var spawn in spawns)
        {
            player!.PrintToChat($"{LogPrefix}Spawn: {spawn.Vector} {spawn.QAngle} {spawn.Team} {spawn.Bombsite} {(spawn.CanBePlanter ? "Y" : "N")}");
        }
    }

    [ConsoleCommand("css_teleport", "This command teleports the player to the given coordinates")]
    [RequiresPermissions("@css/root")]
    public void OnCommandTeleport(CCSPlayerController? player, CommandInfo commandInfo)
    {
        if (!Helpers.IsValidPlayer(player))
        {
            return;
        }
        
        if (!player!.PlayerPawn.IsValid)
        {
            return;
        }
        
        if (commandInfo.ArgCount != 4)
        {
            return;
        }

        if (!float.TryParse(commandInfo.ArgByIndex(1), out var positionX))
        {
            return;
        }

        if (!float.TryParse(commandInfo.ArgByIndex(2), out var positionY))
        {
            return;
        }

        if (!float.TryParse(commandInfo.ArgByIndex(3), out var positionZ))
        {
            return;
        }

        player.PlayerPawn.Value?.Teleport(new Vector(positionX, positionY, positionZ), new QAngle(0f,0f,0f), new Vector(0f, 0f, 0f));
    }

    // Listeners
    private void OnMapStart(string mapName)
    {
        Console.WriteLine($"{LogPrefix}OnMapStart listener triggered!");
        
        // Execute the retakes configuration.
        Helpers.ExecuteRetakesConfiguration();
        
        // If we don't have a map config loaded, load it.
        if (!MapConfig.IsLoaded(_mapConfig, Server.MapName))
        {
            _mapConfig = new MapConfig(ModuleDirectory, Server.MapName);
            _mapConfig.Load();
        }
        
        if (!RetakesConfig.IsLoaded(_retakesConfig))
        {
            _retakesConfig = new RetakesConfig(ModuleDirectory);
            _retakesConfig.Load();
        }
        
        _gameManager = new GameManager(
            new QueueManager(
                _retakesConfig?.RetakesConfigData?.MaxPlayers,
                _retakesConfig?.RetakesConfigData?.TerroristRatio
            ),
            _retakesConfig?.RetakesConfigData?.RoundsToScramble
        );
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
        
        if (Utilities.GetPlayers().ToList().Count <= 2)
        {
            Console.WriteLine($"{LogPrefix}First or second player connected, resetting game.");
            Helpers.RestartGame();
        }

        return HookResult.Continue;
    }
    
    [GameEventHandler]
    public HookResult OnRoundPreStart(EventRoundPrestart @event, GameEventInfo info)
    {
        Console.WriteLine($"{LogPrefix}Round Pre Start event fired!");
        
        // If we are in warmup, skip.
        if (GetGameRules().WarmupPeriod)
        {
            Console.WriteLine($"{LogPrefix}Warmup round, skipping.");
            return HookResult.Continue;
        }
        
        if (_gameManager == null)
        {
            Console.WriteLine($"{LogPrefix}Game manager not loaded.");
            return HookResult.Continue;
        }
        
        if (!_gameManager.QueueManager.ActivePlayers.Any())
        {
            Console.WriteLine($"{LogPrefix}No active players, skipping.");
            _gameManager.QueueManager.SetupActivePlayers();
            return HookResult.Continue;
        }
        
        // Update Queue status
        Console.WriteLine($"{LogPrefix}Updating queues...");
        _gameManager.QueueManager.DebugQueues(true);
        _gameManager.QueueManager.Update();
        _gameManager.QueueManager.DebugQueues(false);
        Console.WriteLine($"{LogPrefix}Updated queues.");
        
        // Reset round teams to allow team changes.
        _gameManager.QueueManager.ClearRoundTeams();
        
        // Handle team swaps during round pre-start.
        switch (_lastRoundWinner)
        {
            case CsTeam.CounterTerrorist:
                Console.WriteLine($"{LogPrefix}Calling CounterTerroristRoundWin()");
                _gameManager.CounterTerroristRoundWin();
                Console.WriteLine($"{LogPrefix}CounterTerroristRoundWin call complete");
                break;
            
            case CsTeam.Terrorist:
                Console.WriteLine($"{LogPrefix}Calling TerroristRoundWin()");
                _gameManager.TerroristRoundWin();
                Console.WriteLine($"{LogPrefix}TerroristRoundWin call complete");
                break;
        }

        _gameManager.BalanceTeams();
        
        // Set round teams to prevent team changes mid round
        _gameManager.QueueManager.SetRoundTeams();

        return HookResult.Continue;
    }

    [GameEventHandler]
    public HookResult OnRoundStart(EventRoundStart @event, GameEventInfo info)
    {
        Console.WriteLine($"{LogPrefix}Round Start event fired!");

        // If we are in warmup, skip.
        if (GetGameRules().WarmupPeriod)
        {
            Console.WriteLine($"{LogPrefix}Warmup round, skipping.");
            return HookResult.Continue;
        }
        
        if (_gameManager == null)
        {
            Console.WriteLine($"{LogPrefix}Game manager not loaded.");
            return HookResult.Continue;
        }
        
        // Reset round state.
        _currentBombsite = Helpers.Random.Next(0, 2) == 0 ? Bombsite.A : Bombsite.B;
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
        foreach (var player in Helpers.Shuffle(_gameManager.QueueManager.ActivePlayers))
        {
            if (!Helpers.IsValidPlayer(player) || (CsTeam)player.TeamNum < CsTeam.Terrorist)
            {
                continue;
            }
            
            var playerPawn = player.PlayerPawn.Value;

            if (playerPawn == null)
            {
                continue;
            }
            
            var isTerrorist = (CsTeam)player.TeamNum == CsTeam.Terrorist;

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
                spawn = Helpers.GetAndRemoveRandomItem(isTerrorist ? tSpawns : ctSpawns);
            }
            
            playerPawn.Teleport(spawn.Vector, spawn.QAngle, new Vector());
        }
        Console.WriteLine($"{LogPrefix}Moving players to spawns COMPLETE.");
        
        AnnounceBombsite(_currentBombsite);
        
        return HookResult.Continue;
    }

    [GameEventHandler]
    public HookResult OnRoundPostStart(EventRoundPoststart @event, GameEventInfo info)
    {
        Console.WriteLine($"{LogPrefix}OnRoundPostStart event fired.");
        
        if (_gameManager == null)
        {
            Console.WriteLine($"{LogPrefix}Game manager not loaded.");
            return HookResult.Continue;
        }
        
        // If we are in warmup, skip.
        if (GetGameRules().WarmupPeriod)
        {
            Console.WriteLine($"{LogPrefix}Warmup round, skipping.");
            return HookResult.Continue;
        }
        
        Console.WriteLine($"{LogPrefix}Trying to loop valid active players.");
        foreach (var player in _gameManager.QueueManager.ActivePlayers.Where(Helpers.IsValidPlayer))
        {
            Console.WriteLine($"{LogPrefix}[{player.PlayerName}] Adding timer for allocation...");

            if (!Helpers.IsValidPlayer(player))
            {
                continue;
            }
            
            // Strip the player of all of their weapons and the bomb before any spawn / allocation occurs.
            Helpers.RemoveHelmetAndHeavyArmour(player);
            Helpers.RemoveAllWeaponsAndEntities(player);

            // Create a timer to do this as it would occasionally fire too early.
            AddTimer(0.05f, () =>
            {
                if (!Helpers.IsValidPlayer(player))
                {
                    Console.WriteLine($"{LogPrefix}Allocating weapons: Player is not valid.");
                    return;
                }
                
                if (!RetakesConfig.IsLoaded(_retakesConfig) || _retakesConfig!.RetakesConfigData!.EnableFallbackAllocation)
                {
                    Console.WriteLine($"{LogPrefix}Allocating...");
                    WeaponsAllocator.Allocate(player);
                    EquipmentAllocator.Allocate(player);
                    GrenadeAllocator.Allocate(player);
                }
                else
                {
                    Console.WriteLine($"{LogPrefix}Fallback allocation disabled, skipping.");
                }

                Console.WriteLine($"{LogPrefix}[{player.PlayerName}] Handling bomb allocation:");
                if ((CsTeam)player.TeamNum == CsTeam.Terrorist)
                {
                    Console.WriteLine($"{LogPrefix}[{player.PlayerName}] is terrorist");
                    Console.WriteLine($"{LogPrefix}[{player.PlayerName}] Removing bomb");
                    // Remove the bomb from the player.
                    player.RemoveItemByDesignerName("weapon_c4", true);

                    if (player == _planter)
                    {
                        Console.WriteLine($"{LogPrefix}[{player.PlayerName}] Player IS planter, giving bomb (player.givenameditem)");
                        Helpers.GiveAndSwitchToBomb(player);
                    }
                }
            });
        }

        return HookResult.Continue;
    }
    
    [GameEventHandler]
    public HookResult OnPlayerSpawn(EventPlayerSpawn @event, GameEventInfo info)
    {
        Console.WriteLine($"{LogPrefix}OnPlayerSpawn event fired.");
        
        if (_gameManager == null)
        {
            Console.WriteLine($"{LogPrefix}Game manager not loaded.");
            return HookResult.Continue;
        }
        
        var player = @event.Userid;

        if (!Helpers.IsValidPlayer(player) || !Helpers.IsPlayerConnected(player))
        {
            return HookResult.Continue;
        }
        
        // debug and check if the player is in the queue.
        Console.WriteLine($"{LogPrefix}[{player.PlayerName}] Checking ActivePlayers.");
        if (!_gameManager.QueueManager.ActivePlayers.Contains(player))
        {
            Console.WriteLine($"{LogPrefix}[{player.PlayerName}] Checking player pawn {player.PlayerPawn.Value != null}.");
            if (player.PlayerPawn.Value != null && player.PlayerPawn.IsValid && player.PlayerPawn.Value.IsValid)
            {
                Console.WriteLine($"{LogPrefix}[{player.PlayerName}] player pawn is valid {player.PlayerPawn.IsValid} && {player.PlayerPawn.Value.IsValid}.");
                Console.WriteLine($"{LogPrefix}[{player.PlayerName}] calling playerpawn.commitsuicide()");
                player.PlayerPawn.Value.CommitSuicide(false, true);
            }
            
            Console.WriteLine($"{LogPrefix}[{player.PlayerName}] Player not in ActivePlayers, moving to spectator.");
            if (!player.IsBot)
            {
                Console.WriteLine($"{LogPrefix}[{player.PlayerName}] moving to spectator.");
                player.ChangeTeam(CsTeam.Spectator);
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

        if (bombEntities.Count > 0)
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
        Console.WriteLine($"{LogPrefix}OnPlayerDeath event fired.");

        if (_gameManager == null)
        {
            Console.WriteLine($"{LogPrefix}Game manager not loaded.");
            return HookResult.Continue;
        }

        var attacker = @event.Attacker;
        var assister = @event.Assister;

        if (Helpers.IsValidPlayer(attacker))
        {
            _gameManager.AddScore(attacker, GameManager.ScoreForKill);
        }

        if (Helpers.IsValidPlayer(assister))
        {
            _gameManager.AddScore(assister, GameManager.ScoreForAssist);
        }

        return HookResult.Continue;
    }
    
    [GameEventHandler]
    public HookResult OnBombDefused(EventBombDefused @event, GameEventInfo info)
    {
        Console.WriteLine($"{LogPrefix}OnBombDefused event fired.");
        
        if (_gameManager == null)
        {
            Console.WriteLine($"{LogPrefix}Game manager not loaded.");
            return HookResult.Continue;
        }
        
        var player = @event.Userid;

        if (Helpers.IsValidPlayer(player))
        {
            _gameManager.AddScore(player, GameManager.ScoreForDefuse);
        }

        return HookResult.Continue;
    }
    
    [GameEventHandler]
    public HookResult OnRoundEnd(EventRoundEnd @event, GameEventInfo info)
    {
        Console.WriteLine($"{LogPrefix}OnRoundEnd event fired.");

        _lastRoundWinner = (CsTeam)@event.Winner;

        return HookResult.Continue;
    }
    
    [GameEventHandler]
    public HookResult OnPlayerTeam(EventPlayerTeam @event, GameEventInfo info)
    {
        Console.WriteLine($"{LogPrefix}OnRoundEnd event fired.");
        
        if (_gameManager == null)
        {
            Console.WriteLine($"{LogPrefix}Game manager not loaded.");
            return HookResult.Continue;
        }
        
        var player = @event.Userid;

        if (!Helpers.IsValidPlayer(player))
        {
            return HookResult.Continue;
        }
        
        Console.WriteLine($"{LogPrefix}[{player.PlayerName}] OnPlayerTeam event fired. ({(@event.Isbot ? "BOT" : "NOT BOT")}) {(CsTeam)@event.Oldteam} -> {(CsTeam)@event.Team}");
        
        _gameManager.QueueManager.DebugQueues(true);
        _gameManager.QueueManager.PlayerTriedToJoinTeam(player, (CsTeam)@event.Oldteam, (CsTeam)@event.Team);
        _gameManager.QueueManager.DebugQueues(false);

        // If we don't have any active players, setup the active players and restart the game.
        if (_gameManager.QueueManager.ActivePlayers.Count == 0)
        {
            _gameManager.QueueManager.SetupActivePlayers();
            Helpers.RestartGame();
        }

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
        
        if (_gameManager == null)
        {
            Console.WriteLine($"{LogPrefix}Game manager not loaded.");
            return HookResult.Continue;
        }
        
        _gameManager.QueueManager.DebugQueues(true);
        _gameManager.QueueManager.PlayerDisconnected(player);
        _gameManager.QueueManager.DebugQueues(false);

        return HookResult.Continue;
    }
    
    public static CCSGameRules GetGameRules()
    {
        var gameRules = Helpers.GetGameRules();
        
        if (gameRules == null)
        {
            throw new Exception($"{LogPrefix}Game rules not found!");
        }
        
        return gameRules;
    }
    
    // Helpers (with localization so they must be in here until I can figure out how to use it elsewhere)
    private void AnnounceBombsite(Bombsite bombsite)
    {
        Console.WriteLine($"{LogPrefix}Announcing bombsite output to all players.");
        
        string[] bombsiteAnnouncers =
        {
            "balkan_epic",
            "leet_epic",
            "professional_epic",
            "professional_fem",
            "seal_epic",
            "swat_epic",
            "swat_fem"
        };

        var isRetakesConfigLoaded = RetakesConfig.IsLoaded(_retakesConfig);
        
        foreach (var player in Utilities.GetPlayers())
        {
            // Print this 3 times to ensure that the player sees it.
            for (var i = 0; i < 3; i++)
            {
                player.PrintToChat(
                $"{MessagePrefix}{Localizer["bombsite.announcement", bombsite == Bombsite.A ? "A" : "B"]}"
                );
            }
            
            if (!isRetakesConfigLoaded || _retakesConfig!.RetakesConfigData!.EnableBombsiteAnnouncementVoices)
            {
                // Do this here so every player hears a random announcer each round.
                var bombsiteAnnouncer = bombsiteAnnouncers[Helpers.Random.Next(bombsiteAnnouncers.Length)];
                
                player.ExecuteClientCommand(
                    $"play sounds/vo/agents/{bombsiteAnnouncer}/loc_{bombsite.ToString().ToLower()}_01");
            }
            
            if (!isRetakesConfigLoaded || _retakesConfig!.RetakesConfigData!.EnableBombsiteAnnouncementCenter)
            {
                player.PrintToCenterHtml(Localizer["bombsite.announcement", bombsite == Bombsite.A ? "A" : "B"]);
            }
        }
        
        Console.WriteLine($"{LogPrefix}Printing bombsite output to all players COMPLETE.");
    }
}
