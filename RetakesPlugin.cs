using System.Drawing;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Utils;
using RetakesPlugin.Modules;
using RetakesPlugin.Modules.Enums;
using RetakesPlugin.Modules.Allocators;
using RetakesPlugin.Modules.Configs;
using RetakesPlugin.Modules.Managers;
using Helpers = RetakesPlugin.Modules.Helpers;

namespace RetakesPlugin;

[MinimumApiVersion(131)]
public class RetakesPlugin : BasePlugin
{
    private const string Version = "1.2.8";
    
    #region Plugin info
    public override string ModuleName => "Retakes Plugin";
    public override string ModuleVersion => Version;
    public override string ModuleAuthor => "B3none";
    public override string ModuleDescription => "Community retakes for CS2.";
    #endregion

    #region Constants
    public static readonly string LogPrefix = $"[Retakes {Version}] ";
    public static readonly string MessagePrefix = $"[{ChatColors.Green}Retakes{ChatColors.White}] ";
    #endregion
    
    #region Helpers
    private Translator _translator;
    private GameManager? _gameManager;
    #endregion
    
    #region Configs
    private MapConfig? _mapConfig;
    private RetakesConfig? _retakesConfig;
    #endregion
    
    #region State
    private Bombsite _currentBombsite = Bombsite.A;
    private CCSPlayerController? _planter;
    private bool _isBombPlanted;
    private CsTeam _lastRoundWinner;
    #endregion
    
    public RetakesPlugin()
    {
        _translator = new Translator(Localizer);
    }

    public override void Load(bool hotReload)
    {
        // Reset this on load
        _translator = new Translator(Localizer);
        
        Console.WriteLine($"{LogPrefix}Plugin loaded!");
        
        RegisterListener<Listeners.OnMapStart>(OnMapStart);
        // RegisterListener<Listeners.OnTick>(OnTick);

        if (hotReload)
        {
            // If a hot reload is detected restart the current map.
            Server.ExecuteCommand($"map {Server.MapName}");
        }
    }

    private CPlantedC4? _plantedC4;
    private const float BombDefuseRange = 62.0f;
    private void OnTick()
    {
        _plantedC4 = Helpers.GetPlantedC4();

        if (_plantedC4 == null)
        {
            return;
        }
        
        foreach (var player in Utilities.GetPlayers())
        {
            var playerPawn = player.PlayerPawn.Value;

            if (playerPawn == null || playerPawn.MovementServices == null || (CsTeam)player.TeamNum != CsTeam.CounterTerrorist)
            {
                continue;
            }
            
            var isHoldingUse = player.Buttons.HasFlag(PlayerButtons.Use);
            
            if (
                isHoldingUse
                && !playerPawn.IsDefusing
                && Helpers.IsInRange(BombDefuseRange, playerPawn.AbsOrigin!, _plantedC4.AbsOrigin!)
                && Helpers.IsOnGround(player)
                && Helpers.IsLookingAtBomb(playerPawn, _plantedC4)
            ) 
            {
                Server.PrintToChatAll($"{MessagePrefix}{player.PlayerName} is in range of the bomb, and is holding use.");
                Helpers.SendBombBeginDefuseEvent(player);
                playerPawn.IsDefusing = true;
            }

            if (!isHoldingUse && playerPawn.IsDefusing)
            {
                Helpers.SendBombAbortDefuseEvent(player);
                playerPawn.IsDefusing = false;
            }
        }
    }

    // Commands
    
    #region Commands
    [ConsoleCommand("css_addspawn", "Adds a spawn point for retakes to the map.")]
    [CommandHelper(minArgs: 2, usage: "[T/CT] [A/B] [Y/N (can be planter / default N)]", whoCanExecute: CommandUsage.CLIENT_ONLY)]
    [RequiresPermissions("@css/root")]
    public void OnCommandAddSpawn(CCSPlayerController? player, CommandInfo commandInfo)
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

    [ConsoleCommand("css_defuser")]
    public void OnCommandDefuser(CCSPlayerController? player, CommandInfo commandInfo)
    {
        Console.WriteLine($"{LogPrefix}Entity command called.");
        
        if (player == null || !player.IsValid)
        {
            return;
        }

        _plantedC4 = Helpers.GetPlantedC4();

        if (_plantedC4 == null)
        {
            return;
        }

        AddTimer(3.0f, () =>
        {
            var playerPawn = player.PlayerPawn.Value!;
            playerPawn.IsDefusing = true;
            playerPawn.ProgressBarStartTime = Server.CurrentTime;
            playerPawn.ProgressBarDuration = 1;
            
            Console.WriteLine($"{LogPrefix}{player.PlayerName}.ProgressBarStartTime: {playerPawn.ProgressBarStartTime}");
            Console.WriteLine($"{LogPrefix}{player.PlayerName}.ProgressBarDuration: {playerPawn.ProgressBarDuration}");
        });
        
        Console.WriteLine($"{LogPrefix} Setting defuser.");
        Schema.SetSchemaValue(_plantedC4.Handle, "CPlantedC4", "m_hBombDefuser", player.PlayerPawn.Index);
    }

    [ConsoleCommand("css_entity")]
    public void OnCommandEntity(CCSPlayerController? player, CommandInfo commandInfo)
    {
        Console.WriteLine($"{MessagePrefix}Entity command called.");
        
        if (player == null || !player.IsValid)
        {
            return;
        }
        
        // Get planted c4
        var plantedC4 = Utilities.FindAllEntitiesByDesignerName<CPlantedC4>("planted_c4").FirstOrDefault();
        
        if (plantedC4 == null || _planter == null)
        {
            Console.WriteLine($"{MessagePrefix}Planted C4 not found or planter not found.");
            return;
        }

        var found = false;
        foreach (var entity in Utilities.GetAllEntities())
        {
            if (entity.Index == plantedC4.ControlPanel.Index)
            {
                Console.WriteLine($"{MessagePrefix}Entity: {entity.DesignerName}");
                found = true;
            }
        }

        if (!found)
        {
            Console.WriteLine($"{MessagePrefix}Entity not found.");
        }
    }

    private CEntityIOOutput? _phys;
    [ConsoleCommand("css_store")]
    public void OnCommandStore(CCSPlayerController? player, CommandInfo commandInfo)
    {
        Console.WriteLine($"{MessagePrefix}Entity command called.");
        
        if (player == null || !player.IsValid)
        {
            return;
        }
        
        // Get planted c4
        var plantedC4 = Utilities.FindAllEntitiesByDesignerName<CPlantedC4>("planted_c4").FirstOrDefault();
        
        if (plantedC4 == null || _planter == null)
        {
            Console.WriteLine($"{MessagePrefix}Planted C4 not found or planter not found.");
            return;
        }

        _phys = plantedC4.OnBombBeginDefuse;
    }

    [ConsoleCommand("css_set")]
    public void OnCommandSet(CCSPlayerController? player, CommandInfo commandInfo)
    {
        Console.WriteLine($"{MessagePrefix}Entity command called.");
        
        if (player == null || !player.IsValid)
        {
            return;
        }
        
        // Get planted c4
        var plantedC4 = Utilities.FindAllEntitiesByDesignerName<CPlantedC4>("planted_c4").FirstOrDefault();
        
        if (plantedC4 == null || _planter == null)
        {
            Console.WriteLine($"{MessagePrefix}Planted C4 not found or planter not found.");
            return;
        }

        if (_phys == null)
        {
            Console.WriteLine($"{MessagePrefix}Phys is null.");
            return;
        }
        
        // Schema.SetSchemaValue(plantedC4.Handle, "CBaseEntity", "m_OnBombBeginDefuse", _phys.Handle);
        // Helpers.AcceptInput(
        //     plantedC4.Handle
        // );
    }

    [ConsoleCommand("css_swap")]
    public void OnCommandSwap(CCSPlayerController? player, CommandInfo commandInfo)
    {
        if (player == null || !player.IsValid)
        {
            return;
        }
        
        player.SwitchTeam((CsTeam)player.TeamNum == CsTeam.CounterTerrorist ? CsTeam.Terrorist : CsTeam.CounterTerrorist);
    }

    [ConsoleCommand("css_lab")]
    public void OnCommandLab(CCSPlayerController? player, CommandInfo commandInfo)
    {
        if (player == null || !player.IsValid || _plantedC4 == null)
        {
            return;
        }

        Helpers.IsLookingAtBomb(player.PlayerPawn.Value!, _plantedC4);
    }

    [ConsoleCommand("css_debugqueues", "Prints the state of the queues to the console.")]
    [CommandHelper(whoCanExecute: CommandUsage.SERVER_ONLY)]
    [RequiresPermissions("@css/root")]
    public void OnCommandDebugState(CCSPlayerController? player, CommandInfo commandInfo)
    {
        if (_gameManager == null)
        {
            Console.WriteLine($"{LogPrefix}Game manager not loaded.");
            return;
        }
        
        _gameManager.QueueManager.DebugQueues(true);
    }

    [ConsoleCommand("css_showqangle", "This command shows the players current QAngle")]
    [RequiresPermissions("@css/root")]
    public void OnCommandShowQangle(CCSPlayerController? player, CommandInfo commandInfo)
    {
        if (!Helpers.IsValidPlayer(player))
        {
            return;
        }

        var playerPawn = player!.PlayerPawn.Value!; 
        var qAngle = playerPawn.AbsRotation;
        var lookTargetPosition = playerPawn.LookTargetPosition;
        var eyeAngles = playerPawn.EyeAngles;

        Server.PrintToChatAll($"{MessagePrefix}lookTargetPosition: x({lookTargetPosition!.X}) y({lookTargetPosition!.Y}) z({lookTargetPosition!.Z})");
        Server.PrintToChatAll($"{MessagePrefix}qAngle: x({qAngle!.X}) y({qAngle!.Y}) z({qAngle!.Z})");
        Server.PrintToChatAll($"{MessagePrefix}eyeAngles: x({eyeAngles!.X}) y({eyeAngles!.Y}) z({eyeAngles!.Z})");
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
        
        foreach (var spawn in spawns)
        {
            // Tell the player about the spawn.
            player!.PrintToChat($"{LogPrefix}Spawn: {spawn.Vector} {spawn.QAngle} {spawn.Team} {spawn.Bombsite} {(spawn.CanBePlanter ? "Y" : "N")}");
            
            // Create beam
            var beam = Utilities.CreateEntityByName<CEnvBeam>("env_beam");

            if (beam == null)
            {
                throw new Exception("Failed to create beam entity.");
            }

            var endBeam = spawn.Vector;
            endBeam.Z = spawn.Vector.Z + 3000; 
            
            Helpers.MoveBeam(beam, spawn.Vector, endBeam);
            beam.SetModel("sprites/laserbeam.vmt");
            beam.Radius = 10;
            beam.StartFrame = 0;
            beam.FrameRate = 0;
            beam.LifeState = 1;
            beam.Width = 1;
            beam.EndWidth = 1;
            beam.Amplitude = 0;
            beam.Speed = 50;
            beam.Flags = 0;
            beam.FadeLength = 0;
            beam.Render = spawn.Team == CsTeam.Terrorist ? Color.Red : Color.Blue;
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
    #endregion
    
    #region Listeners
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
            _translator,
            new QueueManager(
                _translator,
                _retakesConfig?.RetakesConfigData?.MaxPlayers,
                _retakesConfig?.RetakesConfigData?.TerroristRatio
            ),
            _retakesConfig?.RetakesConfigData?.RoundsToScramble,
            _retakesConfig?.RetakesConfigData?.IsScrambleEnabled
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
        
        Console.WriteLine($"{LogPrefix}OnPlayerConnectFull event fired. {Utilities.GetPlayers().ToList().Count} players connected.");
        if (Utilities.GetPlayers().Where(Helpers.IsPlayerConnected).ToList().Count <= 2)
        {
            Console.WriteLine($"{LogPrefix}First or second player connected, resetting game.");
            Helpers.RestartGame();
        }

        return HookResult.Continue;
    }
    
    [GameEventHandler]
    public HookResult OnRoundPreStart(EventRoundPrestart @event, GameEventInfo info)
    {
        // If we are in warmup, skip.
        if (Helpers.GetGameRules().WarmupPeriod)
        {
            Console.WriteLine($"{LogPrefix}Warmup round, skipping.");
            return HookResult.Continue;
        }
        
        if (_gameManager == null)
        {
            Console.WriteLine($"{LogPrefix}Game manager not loaded.");
            return HookResult.Continue;
        }
        
        // Reset round teams to allow team changes.
        _gameManager.QueueManager.ClearRoundTeams();
        
        // Update Queue status
        Console.WriteLine($"{LogPrefix}Updating queues...");
        _gameManager.QueueManager.DebugQueues(true);
        _gameManager.QueueManager.Update();
        _gameManager.QueueManager.DebugQueues(false);
        Console.WriteLine($"{LogPrefix}Updated queues.");
        
        // Handle team swaps during round pre-start.
        switch (_lastRoundWinner)
        {
            case CsTeam.CounterTerrorist:
                Console.WriteLine($"{LogPrefix}Calling CounterTerroristRoundWin()");
                _gameManager.CounterTerroristRoundWin(_planter, _isBombPlanted);
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
        // If we are in warmup, skip.
        if (Helpers.GetGameRules().WarmupPeriod)
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
        if (_gameManager == null)
        {
            Console.WriteLine($"{LogPrefix}Game manager not loaded.");
            return HookResult.Continue;
        }
        
        // If we are in warmup, skip.
        if (Helpers.GetGameRules().WarmupPeriod)
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
                    
                    // Switching to weapon
                    NativeAPI.IssueClientCommand((int)player.UserId!, "slot2; slot1");

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

    // [GameEventHandler]
    // public HookResult OnBombDropped(EventBombDropped @event, GameEventInfo info)
    // {
    //     var player = @event.Userid;
    //     
    //     if (!Helpers.IsValidPlayer(player))
    //     {
    //         return HookResult.Continue;
    //     }
    //     
    //     // Remove the bomb entity and give the player that dropped it the bomb
    //     var bombEntities = Utilities.FindAllEntitiesByDesignerName<CBaseEntity>("weapon_c4").ToList();
    //
    //     if (bombEntities.Count > 0)
    //     {
    //         foreach (var bomb in bombEntities)
    //         {
    //             bomb.Remove();
    //         }
    //     }
    //     
    //     Helpers.GiveAndSwitchToBomb(player);
    //     
    //     return HookResult.Continue;
    // }
    
    [GameEventHandler(HookMode.Pre)]
    public HookResult OnBombPlanted(EventBombPlanted @event, GameEventInfo info)
    {
        _isBombPlanted = true;
        
        Console.WriteLine($"{MessagePrefix}OnBombPlanted event fired");
        HandleBombPlantedOld();
        
        AddTimer(4.1f, () => AnnounceBombsite(_currentBombsite, true));
        
        return HookResult.Continue;
    }

    [GameEventHandler]
    public HookResult OnPlayerDeath(EventPlayerDeath @event, GameEventInfo info)
    {
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
        _lastRoundWinner = (CsTeam)@event.Winner;

        return HookResult.Continue;
    }
    
    [GameEventHandler(HookMode.Pre)]
    public HookResult OnPlayerTeam(EventPlayerTeam @event, GameEventInfo info)
    {
        Console.WriteLine($"{LogPrefix}OnPlayerTeam event fired.");
        
        // Ensure all team join events are silent.
        @event.Silent = true;
        
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
        
        Console.WriteLine($"{LogPrefix}[{player.PlayerName}] {(CsTeam)@event.Oldteam} -> {(CsTeam)@event.Team}");
        
        _gameManager.QueueManager.DebugQueues(true);
        _gameManager.QueueManager.PlayerJoinedTeam(player, (CsTeam)@event.Oldteam, (CsTeam)@event.Team);
        _gameManager.QueueManager.DebugQueues(false);
        
        Console.WriteLine($"{LogPrefix}[{player.PlayerName}] checking to ensure we have active players");
        // If we don't have any active players, setup the active players and restart the game.
        if (_gameManager.QueueManager.ActivePlayers.Count == 0)
        {
            Console.WriteLine($"{LogPrefix}[{player.PlayerName}] clearing round teams to allow team changes");
            _gameManager.QueueManager.ClearRoundTeams();
         
            Console.WriteLine($"{LogPrefix}[{player.PlayerName}] no active players found, calling QueueManager.Update()");
            _gameManager.QueueManager.DebugQueues(true);
            _gameManager.QueueManager.Update();
            _gameManager.QueueManager.DebugQueues(false);
            
            Helpers.RestartGame();
        }

        return HookResult.Continue;
    }
    
    [GameEventHandler]
    public HookResult OnPlayerDisconnect(EventPlayerDisconnect @event, GameEventInfo info)
    {
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
        
        _gameManager.QueueManager.RemovePlayerFromQueues(player);
        
        return HookResult.Continue;
    }
    #endregion
    
    // Helpers (with localization so they must be in here until I can figure out how to use it nicely elsewhere)
    private void AnnounceBombsite(Bombsite bombsite, bool onlyCenter = false)
    {
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

        // Get translation message
        var bombsiteLetter = bombsite == Bombsite.A ? "A" : "B";
        var numTerrorist = Helpers.GetCurrentNumPlayers(CsTeam.Terrorist);
        var numCounterTerrorist = Helpers.GetCurrentNumPlayers(CsTeam.CounterTerrorist);
        
        var isRetakesConfigLoaded = RetakesConfig.IsLoaded(_retakesConfig);
        
        // TODO: Once we implement per client translations this will need to be inside the loop
        var announcementMessage = _translator["bombsite.announcement", bombsiteLetter, numTerrorist, numCounterTerrorist];
        
        foreach (var player in Utilities.GetPlayers())
        {
            if (!onlyCenter)
            {
                // Don't use Server.PrintToChat as it'll add another loop through the players.
                player.PrintToChat($"{MessagePrefix}{announcementMessage}");

                if (!isRetakesConfigLoaded || _retakesConfig!.RetakesConfigData!.EnableBombsiteAnnouncementVoices)
                {
                    // Do this here so every player hears a random announcer each round.
                    var bombsiteAnnouncer = bombsiteAnnouncers[Helpers.Random.Next(bombsiteAnnouncers.Length)];

                    player.ExecuteClientCommand(
                        $"play sounds/vo/agents/{bombsiteAnnouncer}/loc_{bombsite.ToString().ToLower()}_01");
                }
                continue;
            }

            if (isRetakesConfigLoaded && !_retakesConfig!.RetakesConfigData!.EnableBombsiteAnnouncementCenter)
            {
                continue;
            }

            if ((CsTeam)player.TeamNum == CsTeam.CounterTerrorist)
            {
                player.PrintToCenter(announcementMessage);
            }
        }
    }

    [GameEventHandler]
    public HookResult OnRoundFreezeEnd(EventRoundFreezeEnd @event, GameEventInfo info)
    {
        Console.WriteLine($"{LogPrefix}OnFreezeTimeEnd event fired.");
        var bombCarrier = Helpers.GetBombCarrier();

        if (bombCarrier == null)
        {
            Console.WriteLine($"{LogPrefix}Bomb carrier not found.");
            return HookResult.Continue;
        }

        if (!bombCarrier.PlayerPawn.Value!.InBombZone)
        {
            Console.WriteLine($"{LogPrefix}Bomb carrier not in bomb zone.");
            return HookResult.Continue;
        }
        
        CreatePlantedC4(bombCarrier);
        // CreatePlantedC4Old(bombCarrier);

        return HookResult.Continue;
    }
        
    // Autoplant helpers (credit zwolof)
    private void CreatePlantedC4(CCSPlayerController bombCarrier)
    {
        Console.WriteLine($"{LogPrefix}CreatePlantedC4 called...");
        
        var plantedBomb = BombFunctions.ShootSatchelCharge(bombCarrier.PlayerPawn.Value);
        
        Console.WriteLine($"{LogPrefix}CreatePlantedC4 complete.");
    }

    private bool CreatePlantedC4Old(CCSPlayerController bombCarrier)
    {
        Console.WriteLine($"{LogPrefix}removing bomb");
        bombCarrier.RemoveItemByDesignerName("weapon_c4", true);

        if (!Helpers.IsValidPlayer(bombCarrier))
        {
            Console.WriteLine($"{LogPrefix}bomb carrier is not valid player");
            return false;
        }
        
        Helpers.SendBombBeginPlantEvent(bombCarrier, _currentBombsite);
        
        var plantedC4 = Utilities.CreateEntityByName<CPlantedC4>("planted_c4");

        if (plantedC4 == null)
        {
            Console.WriteLine($"{LogPrefix}planted c4 is null");
            return false;
        }

        var playerOrigin = bombCarrier.PlayerPawn.Value!.AbsOrigin;
        var playerRotation = bombCarrier.PlayerPawn.Value!.AbsRotation;

        if (playerOrigin == null || playerRotation == null)
        {
            Console.WriteLine($"{LogPrefix}player origin or player rotation is null");
            return false;
        }
        
        playerOrigin.Z -= bombCarrier.PlayerPawn.Value.Collision.Mins.Z;
        plantedC4.MoveType = MoveType_t.MOVETYPE_NONE;
        plantedC4.Collision.SolidType = SolidType_t.SOLID_NONE;
        plantedC4.Friction = 0.9f;
        plantedC4.DefuseLength = 0.0f;
        
        Console.WriteLine($"{LogPrefix}setting planted c4 props");
        plantedC4.BombTicking = true;
        plantedC4.CannotBeDefused = false;
        plantedC4.BeingDefused = false;
        plantedC4.SourceSoundscapeHash = 2005810340;
        
        if (_planter != null)
        {
            Console.WriteLine($"{LogPrefix}Setting CPlantedC4 m_hOwnerEntity to: {_planter.PlayerPawn.Index}");
            Schema.SetSchemaValue(plantedC4.Handle, "CBaseEntity", "m_hOwnerEntity", _planter.PlayerPawn.Index);
            
            Console.WriteLine($"{LogPrefix}Getting CPlantedC4 m_hOwnerEntity value after: {plantedC4.OwnerEntity.Index}");
        }

        Console.WriteLine($"{LogPrefix}calling dispatch spawn");
        plantedC4.DispatchSpawn();
        
        Console.WriteLine($"{LogPrefix}teleporting prop");
        var bombOrigin = new Vector
        {
            X = playerOrigin.X,
            Y = playerOrigin.Y,
            Z = playerOrigin.Z,
        };
        var bombQAngle = new QAngle
        {
            X = playerRotation.X,
            Y = playerRotation.Y,
            Z = playerRotation.Z
        };

        plantedC4.Teleport(bombOrigin, bombQAngle, new Vector(0, 0, 0));
        
        Console.WriteLine($"{LogPrefix}complete! waiting for next frame");
        
        Server.NextFrame(() =>
        {
            Console.WriteLine($"{LogPrefix}getting bombtargets");
            var bombTargets =
                Utilities.FindAllEntitiesByDesignerName<CBombTarget>("func_bomb_target").ToList();

            if (bombTargets.Any())
            {
                Console.WriteLine($"{LogPrefix}got bomb targets, setting bombplantedhere");

                bombTargets.Where(bombTarget => bombTarget.IsBombSiteB == (_currentBombsite == Bombsite.B))
                    .ToList()
                    .ForEach(bombTarget =>
                    {
                        Console.WriteLine($"{MessagePrefix}actually setting BombPlantedHere for {bombTarget.DesignerName}");
                        bombTarget.BombPlantedHere = true;

                        Console.WriteLine($"{LogPrefix}{bombTarget.MountTarget}");
                        
                        Console.WriteLine($"{MessagePrefix} calling helpers.acceptinput for {bombTarget.DesignerName} (is bombsite b: {bombTarget.IsBombSiteB})");
                        Helpers.AcceptInput(bombTarget.Handle, "BombPlanted", plantedC4.Handle, plantedC4.Handle, "");
                    });
            }

            Console.WriteLine($"{LogPrefix}sending bomb planted event");
            Console.WriteLine($"{LogPrefix} c4blow before: {plantedC4.C4Blow}");
            Helpers.SendBombPlantedEvent(bombCarrier, plantedC4);
            Console.WriteLine($"{LogPrefix} c4blow after: {plantedC4.C4Blow}");
            
            AddTimer(0.1f, () =>
            {
                Console.WriteLine($"{LogPrefix} last think tick: {plantedC4.LastThinkTick}");
            });

            Console.WriteLine($"{LogPrefix}setting ct playerPawn properties");
            foreach (var player in Utilities.GetPlayers().Where(player => player.TeamNum == (int)CsTeam.CounterTerrorist))
            {
                if (player.PlayerPawn.Value == null)
                {
                    continue;
                }

                Console.WriteLine($"{LogPrefix} setting for {player.PlayerName}");
                player.PlayerPawn.Value.RetakesHasDefuseKit = true;
                player.PlayerPawn.Value.IsDefusing = false;
                player.PlayerPawn.Value.LastGivenDefuserTime = 0.0f;
                player.PlayerPawn.Value.InNoDefuseArea = false;
            }
        });

        return true;
    }

    private void HandleBombPlantedOld()
    {
        // Get planted c4
        var plantedC4 = Utilities.FindAllEntitiesByDesignerName<CPlantedC4>("planted_c4").FirstOrDefault();
        
        if (plantedC4 == null || _planter == null)
        {
            Console.WriteLine($"{MessagePrefix}Planted C4 not found or planter not found.");
            return;
        }
        
        plantedC4.C4Blow = Server.CurrentTime + 40.0f;
        plantedC4.InterpolationFrame = 1;
        plantedC4.BombSite = (int)_currentBombsite;
        
        // set game rules
        Console.WriteLine($"{MessagePrefix}setting game rules");
        var gameRules = Helpers.GetGameRules();
        gameRules.RoundWinStatus = 0;
        gameRules.BombDropped = false;
        gameRules.BombPlanted = true;
        gameRules.BombDefused = false;
        gameRules.RetakeRules.BlockersPresent = false;
        gameRules.RetakeRules.RoundInProgress = true;
        gameRules.RetakeRules.BombSite = plantedC4.BombSite;
        
        // Debug planted c4
        List<string> c4NestedProps = new() { "" };
        Console.WriteLine("");
        Console.WriteLine("Planted C4 Props...");
        Helpers.DebugObject("planted_c4", plantedC4, c4NestedProps);
        
        List<string> gameRulesNestedProps = new() { "RetakeRules" };
        Console.WriteLine("");
        Console.WriteLine("Game Rules Props...");
        Helpers.DebugObject("_gameRules", gameRules, gameRulesNestedProps);
        
        Console.WriteLine($"{LogPrefix}pawn entity index: {_planter.PlayerPawn.Index}");
        Console.WriteLine($"{LogPrefix}plantedC4 m_hOwnerEntity index: {plantedC4.OwnerEntity.Index}");
        
        // Console.WriteLine($"{LogPrefix}Adding hook to init VF");
        // BombVirtualFunctions.Init.Hook((dynamicHook) =>
        // {
        //     Console.WriteLine("Init hook called! (SIG IS CORRECT)");
        //     return HookResult.Continue;
        // }, HookMode.Post);
        //
        // Console.WriteLine($"{LogPrefix}Calling init VF");
        //
        // if (_planter.PlayerPawn.Value == null)
        // {
        //     throw new Exception("Player pawn is null.");
        // }
        //
        // // Call init on the planted c4
        // BombFunctions.Init(_planter.PlayerPawn.Value, plantedC4.AbsOrigin ?? new Vector(), plantedC4.AbsOrigin ?? new Vector(), false);
        // Console.WriteLine($"{LogPrefix}init VF complete");
    }
}
