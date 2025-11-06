using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes;
using CounterStrikeSharp.API.Core.Capabilities;
using CounterStrikeSharp.API.Modules.Commands;
using RetakesPluginShared;
using System.Text.Json;

using RetakesPlugin.Configs;
using RetakesPlugin.Configs.JsonConverters;
using RetakesPlugin.Events;
using RetakesPlugin.Managers;
using RetakesPlugin.Modules;
using RetakesPlugin.Services;
using RetakesPlugin.Utils;

using RetakesPlugin.Commands.Admin;
using RetakesPlugin.Commands.MapConfig;
using RetakesPlugin.Commands.Player;
using RetakesPlugin.Commands.SpawnEditor;

namespace RetakesPlugin;

[MinimumApiVersion(345)]
public class RetakesPlugin : BasePlugin, IPluginConfig<BaseConfigs>
{
    public const string Version = "3.0.0-beta";

    #region Plugin Info
    public override string ModuleName => "Retakes Plugin";
    public override string ModuleVersion => Version;
    public override string ModuleAuthor => "B3none";
    public override string ModuleDescription => "https://github.com/b3none/cs2-retakes";
    #endregion

    #region Configuration
    public required BaseConfigs Config { get; set; }

    public void OnConfigParsed(BaseConfigs config)
    {
        Config = config;
        Utils.Logger.Initialize(Config.Debug.IsDebugMode);
        Utils.Logger.LogInfo("Main", "Configuration parsed successfully");
    }
    #endregion

    #region Services & Managers
    private readonly Random _random = new();
    private readonly JsonSerializerOptions _jsonOptions;
    private GameManager? _gameManager;
    private SpawnManager? _spawnManager;
    private BreakerManager? _breakerManager;
    private MapConfigService? _mapConfigService;
    private AllocationService? _allocationService;
    private AnnouncementService? _announcementService;
    private RoundEventHandlers? _roundEventHandlers;
    private PlayerEventHandlers? _playerEventHandlers;
    #endregion

    #region Commands
    // Admin Commands
    private ForceBombsiteCommand? _forceBombsiteCommand;
    private ForceBombsiteStopCommand? _forceBombsiteStopCommand;
    private ScrambleCommand? _scrambleCommand;
    private DebugQueuesCommand? _debugQueuesCommand;

    // Map Config Commands
    private MapConfigCommand? _mapConfigCommand;
    private MapConfigsCommand? _mapConfigsCommand;

    // Player Commands
    private VoicesCommand? _voicesCommand;

    // Spawn Editor Commands
    private ShowSpawnsCommand? _showSpawnsCommand;
    private AddSpawnCommand? _addSpawnCommand;
    private RemoveSpawnCommand? _removeSpawnCommand;
    private NearestSpawnCommand? _nearestSpawnCommand;
    private HideSpawnsCommand? _hideSpawnsCommand;
    #endregion

    #region Capabilities
    public static PluginCapability<IRetakesPluginEventSender> RetakesPluginEventSenderCapability { get; } = new("retakes_plugin:event_sender");
    #endregion

    #region State
    private readonly HashSet<CCSPlayerController> _hasMutedVoices = [];
    #endregion

    public RetakesPlugin()
    {
        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            Converters =
            {
                new VectorJsonConverter(),
                new QAngleJsonConverter()
            }
        };
    }

    public override void Load(bool hotReload)
    {
        Utils.Logger.LogInfo("Main", "Plugin loading...");

        RegisterListener<Listeners.OnMapStart>(OnMapStart);
        AddCommandListener("jointeam", OnCommandJoinTeam);

        var retakesPluginEventSender = new RetakesPluginEventSender();
        Capabilities.RegisterPluginCapability(RetakesPluginEventSenderCapability, () => retakesPluginEventSender);

        // Register event handlers
        RegisterEventHandler<EventPlayerConnectFull>(OnPlayerConnectFull);
        RegisterEventHandler<EventRoundPrestart>(OnRoundPreStart);
        RegisterEventHandler<EventRoundStart>(OnRoundStart);
        RegisterEventHandler<EventRoundPoststart>(OnRoundPostStart);
        RegisterEventHandler<EventRoundFreezeEnd>(OnRoundFreezeEnd);
        RegisterEventHandler<EventRoundEnd>(OnRoundEnd);
        RegisterEventHandler<EventPlayerSpawn>(OnPlayerSpawn);
        RegisterEventHandler<EventPlayerDeath>(OnPlayerDeath);
        RegisterEventHandler<EventBombPlanted>(OnBombPlanted, HookMode.Pre);
        RegisterEventHandler<EventBombDefused>(OnBombDefused);
        RegisterEventHandler<EventPlayerDisconnect>(OnPlayerDisconnect, HookMode.Pre);
        RegisterEventHandler<EventPlayerTeam>(OnPlayerTeam, HookMode.Pre);

        if (hotReload)
        {
            Utils.Logger.LogServer($"Update detected, restarting map...");
            Server.ExecuteCommand($"map {Server.MapName}");
        }

        Utils.Logger.LogInfo("Main", "Plugin loaded successfully");
    }

    #region Map Initialization
    private void OnMapStart(string mapName)
    {
        Utils.Logger.LogInfo("MapStart", $"Map started: {mapName}");

        AddTimer(1.0f, () =>
        {
            ServerHelper.ExecuteRetakesConfiguration(ModuleDirectory);
        });

        InitializeServices(mapName);
    }

    private void InitializeServices(string mapName, string? customMapConfig = null)
    {
        try
        {
            // Initialize MapConfigService
            _mapConfigService = new MapConfigService(ModuleDirectory, customMapConfig ?? mapName, _jsonOptions);
            _mapConfigService.Load();

            // Initialize Managers
            _spawnManager = new SpawnManager(_mapConfigService);
            _allocationService = new AllocationService(_random);

            _gameManager = new GameManager(
                this,
                new QueueManager(
                    this,
                    Config.Game.MaxPlayers,
                    Config.Team.TerroristRatio,
                    Config.Queue.QueuePriorityFlag,
                    Config.Queue.QueueImmunityFlag,
                    Config.Team.ShouldForceEvenTeamsWhenPlayerCountIsMultipleOf10,
                    Config.Team.ShouldPreventTeamChangesMidRound
                ),
                Config.Team.RoundsToScramble,
                Config.Team.IsScrambleEnabled,
                Config.Queue.ShouldRemoveSpectators,
                Config.Team.IsBalanceEnabled
            );

            _breakerManager = new BreakerManager(
                Config.Game.ShouldBreakBreakables,
                Config.Game.ShouldOpenDoors
            );

            _announcementService = new AnnouncementService(
                this,
                _random,
                _hasMutedVoices,
                Config.MapConfig.EnableBombsiteAnnouncementVoices,
                Config.MapConfig.EnableBombsiteAnnouncementCenter
            );

            // Initialize Event Handlers
            _roundEventHandlers = new RoundEventHandlers(
                this,
                _gameManager,
                _spawnManager,
                _breakerManager,
                _allocationService,
                _announcementService,
                Config.Bomb.IsAutoPlantEnabled,
                Config.Game.EnableFallbackAllocation,
                Config.MapConfig.EnableFallbackBombsiteAnnouncement,
                _random,
                _mapConfigService
            );

            _playerEventHandlers = new PlayerEventHandlers(this, _gameManager, _hasMutedVoices);

            // Initialize Commands
            _forceBombsiteCommand = new ForceBombsiteCommand(this, _roundEventHandlers);
            _forceBombsiteStopCommand = new ForceBombsiteStopCommand(this, _roundEventHandlers);
            _scrambleCommand = new ScrambleCommand(this, _gameManager);
            _debugQueuesCommand = new DebugQueuesCommand(this, _gameManager);

            _mapConfigCommand = new MapConfigCommand(this, ModuleDirectory, (configName) =>
            {
                InitializeServices(Server.MapName, configName);
            });
            _mapConfigsCommand = new MapConfigsCommand(this, ModuleDirectory);

            _voicesCommand = new VoicesCommand(this, Config, _hasMutedVoices);

            _showSpawnsCommand = new ShowSpawnsCommand(this, _mapConfigService);
            _addSpawnCommand = new AddSpawnCommand(this, _mapConfigService, _spawnManager, _showSpawnsCommand);
            _removeSpawnCommand = new RemoveSpawnCommand(this, _mapConfigService, _spawnManager, _showSpawnsCommand);
            _nearestSpawnCommand = new NearestSpawnCommand(this, _spawnManager, _showSpawnsCommand);
            _hideSpawnsCommand = new HideSpawnsCommand(this, _showSpawnsCommand);

            // Register all commands
            RegisterCommands();

            Utils.Logger.LogInfo("Services", "All services initialized successfully");
        }
        catch (Exception ex)
        {
            Utils.Logger.LogException("Services", ex);
        }
    }

    private void RegisterCommands()
    {
        if (_forceBombsiteCommand == null || _forceBombsiteStopCommand == null || _scrambleCommand == null || _debugQueuesCommand == null || _mapConfigCommand == null || _mapConfigsCommand == null || _voicesCommand == null || _showSpawnsCommand == null || _addSpawnCommand == null || _removeSpawnCommand == null || _nearestSpawnCommand == null || _hideSpawnsCommand == null)
        {
            Utils.Logger.LogWarning("Commands", "Cannot register commands - command handlers not initialized");
            return;
        }

        // Admin Commands
        AddCommand("css_forcebombsite", "Force the retakes to occur from a single bombsite.", _forceBombsiteCommand.OnCommand);
        AddCommand("css_forcebombsitestop", "Clear the forced bombsite and return back to normal.", _forceBombsiteStopCommand.OnCommand);
        AddCommand("css_scramble", "Sets teams to scramble on the next round.", _scrambleCommand.OnCommand);
        AddCommand("css_scrambleteams", "Sets teams to scramble on the next round.", _scrambleCommand.OnCommand);
        AddCommand("css_debugqueues", "Prints the state of the queues to the console.", _debugQueuesCommand.OnCommand);

        // Map Config Commands
        AddCommand("css_mapconfig", "Forces a specific map config file to load.", _mapConfigCommand.OnCommand);
        AddCommand("css_setmapconfig", "Forces a specific map config file to load.", _mapConfigCommand.OnCommand);
        AddCommand("css_loadmapconfig", "Forces a specific map config file to load.", _mapConfigCommand.OnCommand);
        AddCommand("css_mapconfigs", "Displays a list of available map configs.", _mapConfigsCommand.OnCommand);
        AddCommand("css_viewmapconfigs", "Displays a list of available map configs.", _mapConfigsCommand.OnCommand);
        AddCommand("css_listmapconfigs", "Displays a list of available map configs.", _mapConfigsCommand.OnCommand);

        // Spawn Editor Commands
        AddCommand("css_showspawns", "Show the spawns for the specified bombsite.", _showSpawnsCommand.OnCommand);
        AddCommand("css_spawns", "Show the spawns for the specified bombsite.", _showSpawnsCommand.OnCommand);
        AddCommand("css_edit", "Show the spawns for the specified bombsite.", _showSpawnsCommand.OnCommand);
        AddCommand("css_add", "Creates a new retakes spawn for the bombsite currently shown.", _addSpawnCommand.OnCommand);
        AddCommand("css_addspawn", "Creates a new retakes spawn for the bombsite currently shown.", _addSpawnCommand.OnCommand);
        AddCommand("css_new", "Creates a new retakes spawn for the bombsite currently shown.", _addSpawnCommand.OnCommand);
        AddCommand("css_newspawn", "Creates a new retakes spawn for the bombsite currently shown.", _addSpawnCommand.OnCommand);
        AddCommand("css_remove", "Deletes the nearest retakes spawn.", _removeSpawnCommand.OnCommand);
        AddCommand("css_removespawn", "Deletes the nearest retakes spawn.", _removeSpawnCommand.OnCommand);
        AddCommand("css_delete", "Deletes the nearest retakes spawn.", _removeSpawnCommand.OnCommand);
        AddCommand("css_deletespawn", "Deletes the nearest retakes spawn.", _removeSpawnCommand.OnCommand);
        AddCommand("css_nearestspawn", "Goes to nearest retakes spawn.", _nearestSpawnCommand.OnCommand);
        AddCommand("css_nearest", "Goes to nearest retakes spawn.", _nearestSpawnCommand.OnCommand);
        AddCommand("css_hidespawns", "Exits the spawn editing mode.", _hideSpawnsCommand.OnCommand);
        AddCommand("css_done", "Exits the spawn editing mode.", _hideSpawnsCommand.OnCommand);
        AddCommand("css_exitedit", "Exits the spawn editing mode.", _hideSpawnsCommand.OnCommand);

        // Player Commands
        AddCommand("css_voices", "Toggles whether or not you want to hear bombsite voice announcements.", _voicesCommand.OnCommand);

        Utils.Logger.LogInfo("Commands", "All commands registered successfully");
    }
    #endregion

    #region Event Handlers
    private HookResult OnPlayerConnectFull(EventPlayerConnectFull @event, GameEventInfo info)
    {
        return _playerEventHandlers?.OnPlayerConnectFull(@event, info) ?? HookResult.Continue;
    }

    private HookResult OnRoundPreStart(EventRoundPrestart @event, GameEventInfo info)
    {
        return _roundEventHandlers?.OnRoundPreStart(@event, info) ?? HookResult.Continue;
    }

    private HookResult OnRoundStart(EventRoundStart @event, GameEventInfo info)
    {
        return _roundEventHandlers?.OnRoundStart(@event, info) ?? HookResult.Continue;
    }

    private HookResult OnRoundPostStart(EventRoundPoststart @event, GameEventInfo info)
    {
        return _roundEventHandlers?.OnRoundPostStart(@event, info) ?? HookResult.Continue;
    }

    private HookResult OnRoundFreezeEnd(EventRoundFreezeEnd @event, GameEventInfo info)
    {
        return _roundEventHandlers?.OnRoundFreezeEnd(@event, info) ?? HookResult.Continue;
    }

    private HookResult OnRoundEnd(EventRoundEnd @event, GameEventInfo info)
    {
        return _roundEventHandlers?.OnRoundEnd(@event, info) ?? HookResult.Continue;
    }

    private HookResult OnPlayerSpawn(EventPlayerSpawn @event, GameEventInfo info)
    {
        return _playerEventHandlers?.OnPlayerSpawn(@event, info) ?? HookResult.Continue;
    }

    private HookResult OnPlayerDeath(EventPlayerDeath @event, GameEventInfo info)
    {
        return _playerEventHandlers?.OnPlayerDeath(@event, info) ?? HookResult.Continue;
    }

    private HookResult OnBombPlanted(EventBombPlanted @event, GameEventInfo info)
    {
        return _roundEventHandlers?.OnBombPlanted(@event, info) ?? HookResult.Continue;
    }

    private HookResult OnBombDefused(EventBombDefused @event, GameEventInfo info)
    {
        return _roundEventHandlers?.OnBombDefused(@event, info) ?? HookResult.Continue;
    }

    private HookResult OnPlayerDisconnect(EventPlayerDisconnect @event, GameEventInfo info)
    {
        return _playerEventHandlers?.OnPlayerDisconnect(@event, info) ?? HookResult.Continue;
    }

    private HookResult OnPlayerTeam(EventPlayerTeam @event, GameEventInfo info)
    {
        return _playerEventHandlers?.OnPlayerTeam(@event, info) ?? HookResult.Continue;
    }
    #endregion

    #region Command Handlers
    private HookResult OnCommandJoinTeam(CCSPlayerController? player, CommandInfo commandInfo)
    {
        if (_gameManager == null)
        {
            Utils.Logger.LogWarning("Commands", "Game manager not loaded");
            return HookResult.Continue;
        }

        if (!PlayerHelper.IsValid(player) || commandInfo.ArgCount < 2 ||
            !Enum.TryParse<CounterStrikeSharp.API.Modules.Utils.CsTeam>(commandInfo.GetArg(1), out var toTeam))
        {
            return HookResult.Handled;
        }

        var fromTeam = player!.Team;
        Utils.Logger.LogDebug("Commands", $"[{player.PlayerName}] {fromTeam} -> {toTeam}");

        _gameManager.QueueManager.DebugQueues(true);
        var response = _gameManager.QueueManager.PlayerJoinedTeam(player, fromTeam, toTeam);
        _gameManager.QueueManager.DebugQueues(false);

        if (_gameManager.QueueManager.ActivePlayers.Count == 0)
        {
            Utils.Logger.LogDebug("Commands", "No active players, updating queue and restarting game");
            _gameManager.QueueManager.ClearRoundTeams();
            _gameManager.QueueManager.Update();
            GameRulesHelper.RestartGame();
        }

        return response;
    }
    #endregion

    public override void Unload(bool hotReload)
    {
        Utils.Logger.LogInfo("Main", "Plugin unloading...");
        base.Unload(hotReload);
    }
}
