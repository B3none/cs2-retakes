using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Core.Capabilities;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Utils;
using RetakesPlugin.Modules;
using RetakesPluginShared.Enums;
using RetakesPlugin.Modules.Configs;
using RetakesPlugin.Modules.Managers;
using RetakesPluginShared;
using RetakesPluginShared.Events;
using Helpers = RetakesPlugin.Modules.Helpers;

namespace RetakesPlugin;

[MinimumApiVersion(335)]
public class RetakesPlugin : BasePlugin
{
    private const string Version = "2.1.5";

    #region Plugin info
    public override string ModuleName => "Retakes Plugin";
    public override string ModuleVersion => Version;
    public override string ModuleAuthor => "B3none";
    public override string ModuleDescription => "https://github.com/b3none/cs2-retakes";
    #endregion

    #region Constants
    public static readonly string LogPrefix = $"[Retakes {Version}] ";

    // These two static variables are overwritten in the Load / OnMapStart with config values.
    public static string MessagePrefix = $"[{ChatColors.Green}Retakes{ChatColors.White}] ";
    public static bool IsDebugMode;
    #endregion

    #region Helpers
    private Translator _translator;
    private GameManager? _gameManager;
    private SpawnManager? _spawnManager;
    private BreakerManager? _breakerManager;

    public static PluginCapability<IRetakesPluginEventSender> RetakesPluginEventSenderCapability { get; } = new("retakes_plugin:event_sender");
    #endregion

    #region Configs
    private MapConfig? _mapConfig;
    private RetakesConfig? _retakesConfig;
    #endregion

    #region State
    private Bombsite _currentBombsite = Bombsite.A;
    private CCSPlayerController? _planter;
    private CsTeam _lastRoundWinner = CsTeam.None;
    private Bombsite? _showingSpawnsForBombsite;
    private Bombsite? _forcedBombsite;

    // TODO: We should really store this in SQLite, but for now we'll just store it in memory.
    private readonly HashSet<CCSPlayerController> _hasMutedVoices = [];

    private void ResetState()
    {
        _currentBombsite = Bombsite.A;
        _planter = null;
        _lastRoundWinner = CsTeam.None;
        _showingSpawnsForBombsite = null;
    }
    #endregion

    public RetakesPlugin()
    {
        _translator = new Translator(Localizer);
    }

    public override void Load(bool hotReload)
    {
        _translator = new Translator(Localizer);

        MessagePrefix = _translator["retakes.prefix"];

        Helpers.Debug($"Plugin loaded!");

        RegisterListener<Listeners.OnMapStart>(mapName =>
        {
            OnMapStart(mapName);
        });

        AddCommandListener("jointeam", OnCommandJoinTeam);

        var retakesPluginEventSender = new RetakesPluginEventSender();
        Capabilities.RegisterPluginCapability(RetakesPluginEventSenderCapability, () => retakesPluginEventSender);

        if (hotReload)
        {
            Server.PrintToChatAll($"{LogPrefix}Update detected, restarting map...");
            Server.ExecuteCommand($"map {Server.MapName}");
        }
    }

    #region Commands
    [ConsoleCommand("css_mapconfig", "Forces a specific map config file to load.")]
    [ConsoleCommand("css_setmapconfig", "Forces a specific map config file to load.")]
    [ConsoleCommand("css_loadmapconfig", "Forces a specific map config file to load.")]
    [CommandHelper(minArgs: 1, usage: "[filename]", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    [RequiresPermissions("@css/root")]
    public void OnCommandMapConfig(CCSPlayerController? player, CommandInfo commandInfo)
    {
        if (player != null && !Helpers.IsValidPlayer(player))
        {
            return;
        }

        var mapConfigDirectory = Path.Combine(ModuleDirectory, "map_config");

        if (!Directory.Exists(mapConfigDirectory))
        {
            commandInfo.ReplyToCommand($"{MessagePrefix}No map configs found.");
            return;
        }

        var mapConfigFileName = commandInfo.GetArg(1).Trim().Replace(".json", "");

        var mapConfigFilePath = Path.Combine(mapConfigDirectory, $"{mapConfigFileName}.json");

        if (!File.Exists(mapConfigFilePath))
        {
            commandInfo.ReplyToCommand($"{MessagePrefix}Map config file not found.");
            return;
        }

        OnMapStart(Server.MapName, mapConfigFileName);

        commandInfo.ReplyToCommand($"{MessagePrefix}The new map config has been successfully loaded.");
    }

    [ConsoleCommand("css_mapconfigs", "Displays a list of available map configs.")]
    [ConsoleCommand("css_viewmapconfigs", "Displays a list of available map configs.")]
    [ConsoleCommand("css_listmapconfigs", "Displays a list of available map configs.")]
    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    [RequiresPermissions("@css/root")]
    public void OnCommandMapConfigs(CCSPlayerController? player, CommandInfo commandInfo)
    {
        if (player != null && !Helpers.IsValidPlayer(player))
        {
            return;
        }

        var mapConfigDirectory = Path.Combine(ModuleDirectory, "map_config");

        var files = Directory.GetFiles(mapConfigDirectory);

        // organise files alphabetically
        Array.Sort(files);

        if (!Directory.Exists(mapConfigDirectory) || files.Length == 0)
        {
            commandInfo.ReplyToCommand($"{MessagePrefix}No map configs found.");
            return;
        }

        foreach (var file in files)
        {
            var transformedFile = file
                .Replace($"{mapConfigDirectory}/", "")
                .Replace(".json", "");

            commandInfo.ReplyToCommand($"{MessagePrefix}!mapconfig {transformedFile}");
            player?.PrintToConsole($"{MessagePrefix}!mapconfig {transformedFile}");
        }

        commandInfo.ReplyToCommand($"{MessagePrefix}A list of available map configs has been outputted above.");
    }

    [ConsoleCommand("css_forcebombsite", "Force the retakes to occur from a single bombsite.")]
    [CommandHelper(minArgs: 1, usage: "[A/B]", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    [RequiresPermissions("@css/root")]
    public void OnCommandForceBombsite(CCSPlayerController? player, CommandInfo commandInfo)
    {
        if (player != null && !Helpers.IsValidPlayer(player))
        {
            return;
        }

        var bombsite = commandInfo.GetArg(1).ToUpper();
        if (bombsite != "A" && bombsite != "B")
        {
            commandInfo.ReplyToCommand($"{MessagePrefix}You must specify a bombsite [A / B].");
            return;
        }

        _forcedBombsite = bombsite == "A" ? Bombsite.A : Bombsite.B;

        commandInfo.ReplyToCommand($"{MessagePrefix}The bombsite will now be forced to {_forcedBombsite}.");
    }

    [ConsoleCommand("css_forcebombsitestop", "Clear the forced bombsite and return back to normal.")]
    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    [RequiresPermissions("@css/root")]
    public void OnCommandForceBombsiteStop(CCSPlayerController? player, CommandInfo commandInfo)
    {
        if (player != null && !Helpers.IsValidPlayer(player))
        {
            return;
        }

        _forcedBombsite = null;

        commandInfo.ReplyToCommand($"{MessagePrefix}The bombsite will no longer be forced.");
    }

    [ConsoleCommand("css_showspawns", "Show the spawns for the specified bombsite.")]
    [ConsoleCommand("css_spawns", "Show the spawns for the specified bombsite.")]
    [ConsoleCommand("css_edit", "Show the spawns for the specified bombsite.")]
    [CommandHelper(minArgs: 1, usage: "[A/B]", whoCanExecute: CommandUsage.CLIENT_ONLY)]
    [RequiresPermissions("@css/root")]
    public void OnCommandShowSpawns(CCSPlayerController? player, CommandInfo commandInfo)
    {
        if (player != null && !Helpers.IsValidPlayer(player))
        {
            return;
        }

        var bombsite = commandInfo.GetArg(1).ToUpper();
        if (bombsite != "A" && bombsite != "B")
        {
            commandInfo.ReplyToCommand($"{MessagePrefix}You must specify a bombsite [A / B].");
            return;
        }

        if (_mapConfig == null)
        {
            commandInfo.ReplyToCommand($"{MessagePrefix}Map config not loaded for some reason...");
            return;
        }

        _showingSpawnsForBombsite = bombsite == "A" ? Bombsite.A : Bombsite.B;

        // This will fire the OnRoundStart event listener
        Server.ExecuteCommand("mp_warmup_start");
        Server.ExecuteCommand("mp_warmuptime 120");
        Server.ExecuteCommand("mp_warmup_pausetimer 1");
    }

    [ConsoleCommand("css_add", "Creates a new retakes spawn for the bombsite currently shown.")]
    [ConsoleCommand("css_addspawn", "Creates a new retakes spawn for the bombsite currently shown.")]
    [ConsoleCommand("css_new", "Creates a new retakes spawn for the bombsite currently shown.")]
    [ConsoleCommand("css_newspawn", "Creates a new retakes spawn for the bombsite currently shown.")]
    [CommandHelper(minArgs: 1, usage: "[T/CT] [Y/N can be planter]", whoCanExecute: CommandUsage.CLIENT_ONLY)]
    [RequiresPermissions("@css/root")]
    public void OnCommandAddSpawn(CCSPlayerController? player, CommandInfo commandInfo)
    {
        if (_showingSpawnsForBombsite == null)
        {
            commandInfo.ReplyToCommand($"{MessagePrefix}You can't add a spawn if you're not showing the spawns.");
            return;
        }

        if (_spawnManager == null)
        {
            commandInfo.ReplyToCommand($"{MessagePrefix}Spawn manager not loaded for some reason...");
            return;
        }

        if (!Helpers.DoesPlayerHaveAlivePawn(player))
        {
            commandInfo.ReplyToCommand($"{MessagePrefix}You must have an alive player pawn.");
            return;
        }

        var team = commandInfo.GetArg(1).ToUpper();
        if (team != "T" && team != "CT")
        {
            commandInfo.ReplyToCommand($"{MessagePrefix}You must specify a team [T / CT] - [Value: {team}].");
            return;
        }

        var canBePlanterInput = commandInfo.GetArg(2).ToUpper();
        if (!string.IsNullOrWhiteSpace(canBePlanterInput) && canBePlanterInput != "Y" && canBePlanterInput != "N")
        {
            commandInfo.ReplyToCommand(
                $"{MessagePrefix}Incorrect value passed for can be a planter [Y / N] - [Value: {canBePlanterInput}].");
            return;
        }

        var spawns = _spawnManager.GetSpawns((Bombsite)_showingSpawnsForBombsite);

        var closestDistance = 9999.9;

        foreach (var spawn in spawns)
        {
            var distance = Helpers.GetDistanceBetweenVectors(spawn.Vector, player!.PlayerPawn.Value!.AbsOrigin!);

            if (distance > 128.0 || distance > closestDistance)
            {
                continue;
            }

            closestDistance = distance;
        }

        if (closestDistance <= 72)
        {
            commandInfo.ReplyToCommand($"{MessagePrefix}You are too close to another spawn, move away and try again.");
            return;
        }

        var newSpawn = new Spawn(
            vector: player!.PlayerPawn.Value!.AbsOrigin!,
            qAngle: player!.PlayerPawn.Value!.AbsRotation!
        )
        {
            Team = team == "T" ? CsTeam.Terrorist : CsTeam.CounterTerrorist,
            CanBePlanter = team == "T" && !string.IsNullOrWhiteSpace(canBePlanterInput)
                ? canBePlanterInput == "Y"
                : player.PlayerPawn.Value.InBombZoneTrigger,
            Bombsite = (Bombsite)_showingSpawnsForBombsite
        };
        Helpers.ShowSpawn(newSpawn);

        if (_mapConfig == null)
        {
            commandInfo.ReplyToCommand($"{MessagePrefix}Map config not loaded for some reason...");
            return;
        }

        var didAddSpawn = _mapConfig.AddSpawn(newSpawn);
        if (didAddSpawn)
        {
            _spawnManager.CalculateMapSpawns();
        }

        commandInfo.ReplyToCommand($"{MessagePrefix}{(didAddSpawn ? "Spawn added" : "Error adding spawn")}");
    }

    [ConsoleCommand("css_remove", "Deletes the nearest retakes spawn.")]
    [ConsoleCommand("css_removespawn", "Deletes the nearest retakes spawn.")]
    [ConsoleCommand("css_delete", "Deletes the nearest retakes spawn.")]
    [ConsoleCommand("css_deletespawn", "Deletes the nearest retakes spawn.")]
    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
    [RequiresPermissions("@css/root")]
    public void OnCommandRemoveSpawn(CCSPlayerController? player, CommandInfo commandInfo)
    {
        if (_showingSpawnsForBombsite == null)
        {
            commandInfo.ReplyToCommand($"{MessagePrefix}You can't remove a spawn if you're not showing the spawns.");
            return;
        }

        if (_spawnManager == null)
        {
            commandInfo.ReplyToCommand($"{MessagePrefix}Spawn manager not loaded for some reason...");
            return;
        }

        if (_mapConfig == null)
        {
            commandInfo.ReplyToCommand($"{MessagePrefix}Map config not loaded for some reason...");
            return;
        }

        if (!Helpers.DoesPlayerHaveAlivePawn(player))
        {
            return;
        }

        var spawns = _spawnManager.GetSpawns((Bombsite)_showingSpawnsForBombsite);

        if (spawns.Count == 0)
        {
            commandInfo.ReplyToCommand($"{MessagePrefix}No spawns found.");
            return;
        }

        var closestDistance = 9999.9;
        Spawn? closestSpawn = null;

        foreach (var spawn in spawns)
        {
            var distance = Helpers.GetDistanceBetweenVectors(spawn.Vector, player!.PlayerPawn.Value!.AbsOrigin!);

            if (distance > 128.0 || distance > closestDistance)
            {
                continue;
            }

            closestDistance = distance;
            closestSpawn = spawn;
        }

        if (closestSpawn == null)
        {
            commandInfo.ReplyToCommand($"{MessagePrefix}No spawns found within 128 units.");
            return;
        }

        // Remove the beam entity that is showing for the closest spawn.
        var beamEntities = Utilities.FindAllEntitiesByDesignerName<CBeam>("beam");
        foreach (var beamEntity in beamEntities)
        {
            if (beamEntity.AbsOrigin == null)
            {
                continue;
            }

            if (
                beamEntity.AbsOrigin.Z - closestSpawn.Vector.Z == 0 &&
                beamEntity.AbsOrigin.X - closestSpawn.Vector.X == 0 &&
                beamEntity.AbsOrigin.Y - closestSpawn.Vector.Y == 0
            )
            {
                beamEntity.Remove();
            }
        }

        var didRemoveSpawn = _mapConfig.RemoveSpawn(closestSpawn);
        if (didRemoveSpawn)
        {
            _spawnManager.CalculateMapSpawns();
        }

        commandInfo.ReplyToCommand($"{MessagePrefix}{(didRemoveSpawn ? "Spawn removed" : "Error removing spawn")}");
    }

    [ConsoleCommand("css_nearestspawn", "Goes to nearest retakes spawn.")]
    [ConsoleCommand("css_nearest", "Goes to nearest retakes spawn.")]
    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
    [RequiresPermissions("@css/root")]
    public void OnCommandNearestSpawn(CCSPlayerController? player, CommandInfo commandInfo)
    {
        if (_showingSpawnsForBombsite == null)
        {
            commandInfo.ReplyToCommand($"{MessagePrefix}You can't remove a spawn if you're not showing the spawns.");
            return;
        }

        if (_spawnManager == null)
        {
            commandInfo.ReplyToCommand($"{MessagePrefix}Spawn manager not loaded for some reason...");
            return;
        }

        if (_mapConfig == null)
        {
            commandInfo.ReplyToCommand($"{MessagePrefix}Map config not loaded for some reason...");
            return;
        }

        if (!Helpers.DoesPlayerHaveAlivePawn(player))
        {
            return;
        }

        var spawns = _spawnManager.GetSpawns((Bombsite)_showingSpawnsForBombsite);

        if (spawns.Count == 0)
        {
            commandInfo.ReplyToCommand($"{MessagePrefix}No spawns found.");
            return;
        }

        var closestDistance = 9999.9;
        Spawn? closestSpawn = null;

        foreach (var spawn in spawns)
        {
            var distance = Helpers.GetDistanceBetweenVectors(spawn.Vector, player!.PlayerPawn.Value!.AbsOrigin!);

            if (distance > 128.0 || distance > closestDistance)
            {
                continue;
            }

            closestDistance = distance;
            closestSpawn = spawn;
        }

        if (closestSpawn == null)
        {
            commandInfo.ReplyToCommand($"{MessagePrefix}No spawns found within 128 units.");
            return;
        }

        player!.PlayerPawn.Value!.Teleport(closestSpawn.Vector, closestSpawn.QAngle, new Vector());
        commandInfo.ReplyToCommand($"{MessagePrefix}Teleported to nearest spawn");
    }

    [ConsoleCommand("css_hidespawns", "Exits the spawn editing mode.")]
    [ConsoleCommand("css_done", "Exits the spawn editing mode.")]
    [ConsoleCommand("css_exitedit", "Exits the spawn editing mode.")]
    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
    [RequiresPermissions("@css/root")]
    public void OnCommandHideSpawns(CCSPlayerController? player, CommandInfo commandInfo)
    {
        _showingSpawnsForBombsite = null;
        Server.ExecuteCommand("mp_warmup_end");
    }

    [ConsoleCommand("css_scramble", "Sets teams to scramble on the next round.")]
    [ConsoleCommand("css_scrambleteams", "Sets teams to scramble on the next round.")]
    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    [RequiresPermissions("@css/admin")]
    public void OnCommandScramble(CCSPlayerController? player, CommandInfo commandInfo)
    {
        if (_gameManager == null)
        {
            commandInfo.ReplyToCommand($"{MessagePrefix}Game manager not loaded.");
            return;
        }

        _gameManager.ScrambleNextRound(player);
    }

    [ConsoleCommand("css_debugqueues", "Prints the state of the queues to the console.")]
    [CommandHelper(whoCanExecute: CommandUsage.SERVER_ONLY)]
    [RequiresPermissions("@css/root")]
    public void OnCommandDebugState(CCSPlayerController? player, CommandInfo commandInfo)
    {
        if (_gameManager == null)
        {
            Helpers.Debug($"Game manager not loaded.");
            return;
        }

        _gameManager.QueueManager.DebugQueues(true);
    }

    [ConsoleCommand("css_voices", "Toggles whether or not you want to hear bombsite voice announcements.")]
    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void OnCommandVoices(CCSPlayerController? player, CommandInfo commandInfo)
    {
        if (!Helpers.IsValidPlayer(player))
        {
            commandInfo.ReplyToCommand($"{MessagePrefix}You must be a valid player to use this command.");
            return;
        }

        if (RetakesConfig.IsLoaded(_retakesConfig) && !_retakesConfig!.RetakesConfigData!.EnableBombsiteAnnouncementVoices)
        {
            commandInfo.ReplyToCommand($"{MessagePrefix}Bombsite voice announcements are permanently disabled on this server.");
            return;
        }

        var didMute = false;
        if (!_hasMutedVoices.Contains(player!))
        {
            didMute = true;
            _hasMutedVoices.Add(player!);
        }
        else
        {
            _hasMutedVoices.Remove(player!);
        }

        commandInfo.ReplyToCommand($"{MessagePrefix}{_translator["retakes.voices.toggle", didMute ? $"{ChatColors.Red}disabled{ChatColors.White}" : $"{ChatColors.Green}enabled{ChatColors.White}"]}");
    }
    #endregion

    #region Listeners
    private void OnMapStart(string mapName, string? customMapConfig = null)
    {
        Helpers.Debug("OnMapStart listener triggered!");

        ResetState();

        AddTimer(1.0f, () =>
        {
            // Execute the retakes configuration.
            Helpers.ExecuteRetakesConfiguration(ModuleDirectory);
        });

        // If we don't have a map config loaded, load it.
        if (!MapConfig.IsLoaded(_mapConfig, customMapConfig ?? mapName))
        {
            _mapConfig = new MapConfig(ModuleDirectory, customMapConfig ?? mapName);
            _mapConfig.Load();
        }

        if (!RetakesConfig.IsLoaded(_retakesConfig))
        {
            _retakesConfig = new RetakesConfig(ModuleDirectory);
            _retakesConfig.Load();
        }

        if (_mapConfig == null)
        {
            throw new Exception("Map config is null");
        }

        _spawnManager = new SpawnManager(_mapConfig);

        _gameManager = new GameManager(
            _translator,
            new QueueManager(
                _translator,
                _retakesConfig?.RetakesConfigData?.MaxPlayers,
                _retakesConfig?.RetakesConfigData?.TerroristRatio,
                _retakesConfig?.RetakesConfigData?.QueuePriorityFlag,
                _retakesConfig?.RetakesConfigData?.QueueImmunityFlag,
                _retakesConfig?.RetakesConfigData?.ShouldForceEvenTeamsWhenPlayerCountIsMultipleOf10,
                _retakesConfig?.RetakesConfigData?.ShouldPreventTeamChangesMidRound
            ),
            _retakesConfig?.RetakesConfigData?.RoundsToScramble,
            _retakesConfig?.RetakesConfigData?.IsScrambleEnabled,
            _retakesConfig?.RetakesConfigData?.ShouldRemoveSpectators,
            _retakesConfig?.RetakesConfigData?.IsBalanceEnabled
        );

        _breakerManager = new BreakerManager(
            _retakesConfig?.RetakesConfigData?.ShouldBreakBreakables,
            _retakesConfig?.RetakesConfigData?.ShouldOpenDoors
        );

        IsDebugMode = _retakesConfig?.RetakesConfigData?.IsDebugMode ?? false;
    }

    [GameEventHandler]
    public HookResult OnPlayerConnectFull(EventPlayerConnectFull @event, GameEventInfo info)
    {
        var player = @event.Userid;

        if (!Helpers.IsValidPlayer(player))
        {
            return HookResult.Continue;
        }

        // TODO: We can make use of sv_human_autojoin_team 3 to prevent needing to do this.
        player.ForceTeamTime = 3600.0f;

        // Create a timer to do this as it would occasionally fire too early.
        AddTimer(1.0f, () =>
        {
            if (!player.IsValid)
            {
                return;
            }

            player.ChangeTeam(CsTeam.Spectator);
            player.ExecuteClientCommand("teammenu");
        });

        // Many hours of hard work went into this.
        if (new List<ulong> { 76561198028510846, 76561198044886803, 76561198414501446 }.Contains(player.SteamID))
        {
            var grant = _retakesConfig?.RetakesConfigData?.QueuePriorityFlag.Split(",")[0].Trim() ?? "@css/vip";
            player.PrintToConsole($"{LogPrefix}You have been given queue priority {grant} for being a Retakes contributor!");
            AdminManager.AddPlayerPermissions(player, grant);
        }

        return HookResult.Continue;
    }

    [GameEventHandler]
    public HookResult OnRoundPreStart(EventRoundPrestart @event, GameEventInfo info)
    {
        // If we are in warmup, skip.
        if (Helpers.GetGameRules().WarmupPeriod)
        {
            Helpers.Debug($"Warmup round, skipping.");
            return HookResult.Continue;
        }

        if (_gameManager == null)
        {
            Helpers.Debug($"Game manager not loaded.");
            return HookResult.Continue;
        }

        // Reset round teams to allow team changes.
        _gameManager.QueueManager.ClearRoundTeams();

        // Update Queue status
        Helpers.Debug($"Updating queues...");
        _gameManager.QueueManager.DebugQueues(true);
        _gameManager.QueueManager.Update();
        _gameManager.QueueManager.DebugQueues(false);
        Helpers.Debug($"Updated queues.");

        Helpers.Debug($"Calling GameManager.OnRoundPreStart({_lastRoundWinner})");
        _gameManager.OnRoundPreStart(_lastRoundWinner);
        Helpers.Debug($"GameManager.OnRoundPreStart call complete");

        // Set round teams to prevent team changes mid round
        _gameManager.QueueManager.SetRoundTeams();

        return HookResult.Continue;
    }

    [GameEventHandler]
    public HookResult OnRoundStart(EventRoundStart @event, GameEventInfo info)
    {
        // TODO: FIGURE OUT WHY THE FUCK I NEED TO DO THIS
        var weirdAliveSpectators = Utilities.GetPlayers()
            .Where(x => x is { TeamNum: < (int)CsTeam.Terrorist, PawnIsAlive: true });
        foreach (var weirdAliveSpectator in weirdAliveSpectators)
        {
            // I **think** it's caused by auto team balance being on, so turn it off
            Server.ExecuteCommand("mp_autoteambalance 0");
            weirdAliveSpectator.CommitSuicide(false, true);
        }

        // If we are in warmup, skip.
        if (Helpers.GetGameRules().WarmupPeriod)
        {
            Helpers.Debug($"Warmup round, skipping.");

            if (_mapConfig != null)
            {
                Helpers.ShowSpawns(_mapConfig.GetSpawnsClone(), _showingSpawnsForBombsite);
            }

            return HookResult.Continue;
        }

        if (_gameManager == null)
        {
            Helpers.Debug($"Game manager not loaded.");
            return HookResult.Continue;
        }

        if (_spawnManager == null)
        {
            Helpers.Debug($"Spawn manager not loaded.");
            return HookResult.Continue;
        }

        // Reset round state.
        _breakerManager?.Handle();
        _currentBombsite = _forcedBombsite ?? (Helpers.Random.Next(0, 2) == 0 ? Bombsite.A : Bombsite.B);
        _gameManager.ResetPlayerScores();

        Helpers.Debug("Clearing _showingSpawnsForBombsite");
        _showingSpawnsForBombsite = null;

        _planter = _spawnManager.HandleRoundSpawns(_currentBombsite, _gameManager.QueueManager.ActivePlayers);

        if (!RetakesConfig.IsLoaded(_retakesConfig) ||
            _retakesConfig!.RetakesConfigData!.EnableFallbackBombsiteAnnouncement)
        {
            AnnounceBombsite(_currentBombsite);
        }

        RetakesPluginEventSenderCapability.Get()?.TriggerEvent(new AnnounceBombsiteEvent(_currentBombsite));

        return HookResult.Continue;
    }

    [GameEventHandler]
    public HookResult OnRoundPostStart(EventRoundPoststart @event, GameEventInfo info)
    {
        if (_gameManager == null)
        {
            Helpers.Debug($"Game manager not loaded.");
            return HookResult.Continue;
        }

        // If we are in warmup, skip.
        if (Helpers.GetGameRules().WarmupPeriod)
        {
            Helpers.Debug($"Warmup round, skipping.");
            return HookResult.Continue;
        }

        Helpers.Debug($"Trying to loop valid active players.");
        foreach (var player in _gameManager.QueueManager.ActivePlayers.Where(Helpers.IsValidPlayer))
        {
            Helpers.Debug($"[{player.PlayerName}] Handling allocation...");

            if (!Helpers.IsValidPlayer(player))
            {
                continue;
            }

            // Strip the player of all of their weapons and the bomb before any spawn / allocation occurs.
            Helpers.RemoveHelmetAndHeavyArmour(player);
            player.RemoveWeapons();

            if (player == _planter && RetakesConfig.IsLoaded(_retakesConfig) &&
                !_retakesConfig!.RetakesConfigData!.IsAutoPlantEnabled)
            {
                Helpers.Debug($"Player is planter and auto plant is disabled, allocating bomb.");
                Helpers.GiveAndSwitchToBomb(player);
            }

            if (!RetakesConfig.IsLoaded(_retakesConfig) ||
                _retakesConfig!.RetakesConfigData!.EnableFallbackAllocation)
            {
                Helpers.Debug($"Allocating...");
                AllocationManager.Allocate(player);
            }
            else
            {
                Helpers.Debug($"Fallback allocation disabled, skipping.");
            }
        }

        RetakesPluginEventSenderCapability.Get()?.TriggerEvent(new AllocateEvent());

        return HookResult.Continue;
    }

    [GameEventHandler]
    public HookResult OnRoundFreezeEnd(EventRoundFreezeEnd @event, GameEventInfo info)
    {
        // If we are in warmup, skip.
        if (Helpers.GetGameRules().WarmupPeriod)
        {
            Helpers.Debug($"Warmup round, skipping.");
            return HookResult.Continue;
        }

        if (Helpers.GetCurrentNumPlayers(CsTeam.Terrorist) > 0)
        {
            HandleAutoPlant();
        }

        return HookResult.Continue;
    }

    [GameEventHandler]
    public HookResult OnPlayerSpawn(EventPlayerSpawn @event, GameEventInfo info)
    {
        if (_gameManager == null)
        {
            Helpers.Debug($"Game manager not loaded.");
            return HookResult.Continue;
        }

        var player = @event.Userid;

        if (!Helpers.IsValidPlayer(player) || !Helpers.IsPlayerConnected(player))
        {
            return HookResult.Continue;
        }

        // debug and check if the player is in the queue.
        Helpers.Debug($"[{player.PlayerName}] Checking ActivePlayers.");
        if (!_gameManager.QueueManager.ActivePlayers.Contains(player))
        {
            Helpers.Debug($"[{player.PlayerName}] Checking player pawn {player.PlayerPawn.Value != null}.");
            if (player.PlayerPawn.Value != null && player.PlayerPawn.IsValid && player.PlayerPawn.Value.IsValid)
            {
                Helpers.Debug(
                    $"[{player.PlayerName}] player pawn is valid {player.PlayerPawn.IsValid} && {player.PlayerPawn.Value.IsValid}.");
                Helpers.Debug($"[{player.PlayerName}] calling playerpawn.commitsuicide()");
                player.PlayerPawn.Value.CommitSuicide(false, true);
            }

            Helpers.Debug($"[{player.PlayerName}] Player not in ActivePlayers, moving to spectator.");
            if (!player.IsBot)
            {
                Helpers.Debug($"[{player.PlayerName}] moving to spectator.");
                player.ChangeTeam(CsTeam.Spectator);
            }
            if (player.IsBot && !player.IsHLTV)
            {
                _gameManager.QueueManager.ActivePlayers.Add(player);
                Helpers.Debug($"[{player.PlayerName}] Force added bot to active players.");
            }

            return HookResult.Continue;
        }
        else
        {
            Helpers.Debug($"[{player.PlayerName}] Player is in ActivePlayers.");
        }

        return HookResult.Continue;
    }

    [GameEventHandler(HookMode.Pre)]
    public HookResult OnBombPlanted(EventBombPlanted @event, GameEventInfo info)
    {
        Helpers.Debug($"OnBombPlanted event fired");

        AddTimer(4.1f, () => AnnounceBombsite(_currentBombsite, true));

        return HookResult.Continue;
    }

    [GameEventHandler]
    public HookResult OnPlayerDeath(EventPlayerDeath @event, GameEventInfo info)
    {
        if (_gameManager == null)
        {
            Helpers.Debug($"Game manager not loaded.");
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
            Helpers.Debug($"Game manager not loaded.");
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
        // Ensure all team join events are silent.
        @event.Silent = true;

        if (_gameManager == null)
        {
            Helpers.Debug($"Game manager not loaded.");
            return HookResult.Continue;
        }

        return _gameManager.RemoveSpectators(@event, _hasMutedVoices);
    }

    private HookResult OnCommandJoinTeam(CCSPlayerController? player, CommandInfo commandInfo)
    {
        if (_gameManager == null)
        {
            Helpers.Debug($"Game manager not loaded.");
            return HookResult.Continue;
        }

        if (
            !Helpers.IsValidPlayer(player)
            || commandInfo.ArgCount < 2
            || !Enum.TryParse<CsTeam>(commandInfo.GetArg(1), out var toTeam)
        )
        {
            return HookResult.Handled;
        }

        var fromTeam = player!.Team;

        Helpers.Debug($"[{player.PlayerName}] {fromTeam} -> {toTeam}");

        _gameManager.QueueManager.DebugQueues(true);
        var response = _gameManager.QueueManager.PlayerJoinedTeam(player, fromTeam, toTeam);
        _gameManager.QueueManager.DebugQueues(false);

        Helpers.Debug($"[{player.PlayerName}] checking to ensure we have active players");
        // If we don't have any active players, setup the active players and restart the game.
        if (_gameManager.QueueManager.ActivePlayers.Count == 0)
        {
            Helpers.Debug($"[{player.PlayerName}] clearing round teams to allow team changes");
            _gameManager.QueueManager.ClearRoundTeams();

            Helpers.Debug(
                $"[{player.PlayerName}] no active players found, calling QueueManager.Update()");
            _gameManager.QueueManager.DebugQueues(true);
            _gameManager.QueueManager.Update();
            _gameManager.QueueManager.DebugQueues(false);

            Helpers.RestartGame();
        }

        return response;
    }

    [GameEventHandler(HookMode.Pre)]
    public HookResult OnPlayerDisconnect(EventPlayerDisconnect @event, GameEventInfo info)
    {
        var player = @event.Userid;

        if (player == null)
        {
            return HookResult.Continue;
        }

        if (_gameManager == null)
        {
            Helpers.Debug($"Game manager not loaded.");
            return HookResult.Continue;
        }

        _gameManager.QueueManager.RemovePlayerFromQueues(player);
        _hasMutedVoices.Remove(player);

        return HookResult.Continue;
    }
    #endregion

    // Helpers (with localization so they must be in here until I can figure out how to use it nicely elsewhere)
    private void AnnounceBombsite(Bombsite bombsite, bool onlyCenter = false)
    {
        string[] bombsiteAnnouncers =
        [
            "balkan_epic",
            "leet_epic",
            "professional_epic",
            "professional_fem",
            "seal_epic",
            "swat_epic",
            "swat_fem"
        ];

        // Get translation message
        var numTerrorist = Helpers.GetCurrentNumPlayers(CsTeam.Terrorist);
        var numCounterTerrorist = Helpers.GetCurrentNumPlayers(CsTeam.CounterTerrorist);

        var isRetakesConfigLoaded = RetakesConfig.IsLoaded(_retakesConfig);

        // TODO: Once we implement per client translations this will need to be inside the loop
        var announcementMessage = _translator["retakes.bombsite.announcement", bombsite.ToString(), numTerrorist,
            numCounterTerrorist];
        var centerAnnouncementMessage = _translator["center.retakes.bombsite.announcement", bombsite.ToString(), numTerrorist,
            numCounterTerrorist];

        foreach (var player in Utilities.GetPlayers())
        {
            if (!onlyCenter)
            {
                // Don't use Server.PrintToChat as it'll add another loop through the players.
                player.PrintToChat($"{MessagePrefix}{announcementMessage}");

                if (
                    (!isRetakesConfigLoaded || _retakesConfig!.RetakesConfigData!.EnableBombsiteAnnouncementVoices)
                    && !_hasMutedVoices.Contains(player)
                )
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

            if (player.Team == CsTeam.CounterTerrorist)
            {
                player.PrintToCenter(centerAnnouncementMessage);
            }
        }
    }

    private void HandleAutoPlant()
    {
        if (RetakesConfig.IsLoaded(_retakesConfig) && !_retakesConfig!.RetakesConfigData!.IsAutoPlantEnabled)
        {
            return;
        }

        if (_planter != null && Helpers.IsValidPlayer(_planter))
        {
            Helpers.PlantTickingBomb(_planter, _currentBombsite);
        }
        else
        {
            Helpers.TerminateRound(RoundEndReason.RoundDraw);
        }
    }
}
