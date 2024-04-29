using System.Drawing;
using System.Text;
using System.Text.Json;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Utils;
using RetakesPlugin.Modules.Configs;
using RetakesPlugin.Modules.Configs.JsonConverters;
using RetakesPluginShared.Enums;

namespace RetakesPlugin.Modules;

public static class Helpers
{
    internal static readonly Random Random = new();
    internal static readonly JsonSerializerOptions JsonSerializerOptions = new()
    {
        WriteIndented = true,
        Converters =
        {
            new VectorJsonConverter(),
            new QAngleJsonConverter()
        }
    };

    public static bool IsValidPlayer(CCSPlayerController? player)
    {
        return player != null && player.IsValid;
    }

    public static bool DoesPlayerHaveAlivePawn(CCSPlayerController? player, bool shouldBeAlive = true)
    {
        if (!IsValidPlayer(player))
        {
            return false;
        }

        var playerPawn = player!.PlayerPawn.Value;

        if (playerPawn == null || playerPawn is { AbsOrigin: null, AbsRotation: null })
        {
            return false;
        }

        if (shouldBeAlive && !(playerPawn.Health > 0))
        {
            return false;
        }

        return true;
    }

    public static List<T> Shuffle<T>(IEnumerable<T> list)
    {
        var shuffledList = new List<T>(list); // Create a copy of the original list

        var n = shuffledList.Count;
        while (n > 1)
        {
            n--;
            var k = Random.Next(n + 1);
            T value = shuffledList[k];
            shuffledList[k] = shuffledList[n];
            shuffledList[n] = value;
        }

        return shuffledList;
    }

    public static CCSGameRules GetGameRules()
    {
        var gameRulesEntities = Utilities.FindAllEntitiesByDesignerName<CCSGameRulesProxy>("cs_gamerules");
        var gameRules = gameRulesEntities.First().GameRules;

        if (gameRules == null)
        {
            throw new Exception($"{RetakesPlugin.LogPrefix}Game rules not found!");
        }

        return gameRules;
    }

    public static bool IsPlayerConnected(CCSPlayerController player)
    {
        return player.Connected == PlayerConnectedState.PlayerConnected;
    }

    private const string RetakesCfgDirectory = "/../../../../cfg/cs2-retakes";
    private const string RetakesCfgPath = $"{RetakesCfgDirectory}/retakes.cfg";

    public static void ExecuteRetakesConfiguration(string moduleDirectory)
    {
        if (!File.Exists(moduleDirectory + RetakesCfgPath))
        {
            // make any directories required too
            Directory.CreateDirectory(moduleDirectory + RetakesCfgDirectory);

            var retakesCfg = File.Create(moduleDirectory + RetakesCfgPath);

            var retakesCfgContents = @"
                // Things you shouldn't change:
                bot_kick
                bot_quota 0
                mp_autoteambalance 0
                mp_forcecamera 1
                mp_give_player_c4 0
                mp_halftime 0
                mp_ignore_round_win_conditions 0
                mp_join_grace_time 0
                mp_match_can_clinch 0
                mp_maxmoney 0
                mp_playercashawards 0
                mp_respawn_on_death_ct 0
                mp_respawn_on_death_t 0
                mp_solid_teammates 1
                mp_teamcashawards 0
                mp_warmup_pausetimer 0
                sv_skirmish_id 0

                // Things you can change, and may want to:
                mp_roundtime_defuse 0.25
                mp_autokick 0
                mp_c4timer 40
                mp_freezetime 1
                mp_friendlyfire 0
                mp_round_restart_delay 2
                sv_talk_enemy_dead 0
                sv_talk_enemy_living 0
                sv_deadtalk 1
                spec_replay_enable 0
                mp_maxrounds 30
                mp_match_end_restart 0
                mp_timelimit 0
                mp_match_restart_delay 10
                mp_death_drop_gun 1
                mp_death_drop_defuser 1
                mp_death_drop_grenade 1
                mp_warmuptime 15

                echo [Retakes] Config loaded!
            ";

            var retakesCfgBytes = Encoding.UTF8.GetBytes(retakesCfgContents);
            retakesCfg.Write(retakesCfgBytes, 0, retakesCfgBytes.Length);

            retakesCfg.Close();
        }

        Server.ExecuteCommand("exec cs2-retakes/retakes.cfg");
    }

    public static void Debug(string message)
    {
        if (RetakesPlugin.IsDebugMode)
        {
            Console.WriteLine($"{RetakesPlugin.LogPrefix}{message}");
        }
    }

    public static int GetCurrentNumPlayers(CsTeam? csTeam = null)
    {
        var players = 0;

        foreach (var player in Utilities.GetPlayers()
                     .Where(player => IsValidPlayer(player) && IsPlayerConnected(player)))
        {
            if (csTeam == null || player.Team == csTeam)
            {
                players++;
            }
        }

        return players;
    }

    public static void GiveAndSwitchToBomb(CCSPlayerController player)
    {
        player.GiveNamedItem(CsItem.Bomb);
        NativeAPI.IssueClientCommand((int)player.UserId!, "slot5");
    }

    public static void RemoveHelmetAndHeavyArmour(CCSPlayerController player)
    {
        if (player.PlayerPawn.Value == null || player.PlayerPawn.Value.ItemServices == null)
        {
            return;
        }

        var itemServices = new CCSPlayer_ItemServices(player.PlayerPawn.Value.ItemServices.Handle);
        itemServices.HasHelmet = false;
        itemServices.HasHeavyArmor = false;
    }

    public static void RestartGame()
    {
        if (!GetGameRules().WarmupPeriod)
        {
            CheckRoundDone();
        }

        Server.ExecuteCommand("mp_restartgame 1");
    }

    public static void CheckRoundDone()
    {
        var tHumanCount = GetCurrentNumPlayers(CsTeam.Terrorist);
        var ctHumanCount = GetCurrentNumPlayers(CsTeam.CounterTerrorist);

        if (tHumanCount == 0 || ctHumanCount == 0)
        {
            TerminateRound(RoundEndReason.TerroristsWin);
        }
    }

    public static void TerminateRound(RoundEndReason roundEndReason)
    {
        // TODO: Once the signature is updated then this will be redundant
        try
        {
            GetGameRules().TerminateRound(0.1f, roundEndReason);
        }
        catch
        {
            Debug(
                $"Incorrect signature detected (Can't use TerminateRound) killing all alive players instead.");
            var alivePlayers = Utilities.GetPlayers()
                .Where(IsValidPlayer)
                .Where(player => player.PawnIsAlive)
                .ToList();

            foreach (var player in alivePlayers)
            {
                player.CommitSuicide(false, true);
            }
        }
    }

    public static double GetDistanceBetweenVectors(Vector v1, Vector v2)
    {
        var dx = v1.X - v2.X;
        var dy = v1.Y - v2.Y;

        return Math.Sqrt(Math.Pow(dx, 2) + Math.Pow(dy, 2));
    }

    public static void PlantTickingBomb(CCSPlayerController? player, Bombsite bombsite)
    {
        if (player == null || !player.IsValid)
        {
            throw new Exception("Player controller is not valid");
        }

        var playerPawn = player.PlayerPawn.Value;

        if (playerPawn == null || !playerPawn.IsValid)
        {
            throw new Exception("Player pawn is not valid");
        }

        if (playerPawn.AbsOrigin == null)
        {
            throw new Exception("Player pawn abs origin is null");
        }

        if (playerPawn.AbsRotation == null)
        {
            throw new Exception("Player pawn abs rotation is null");
        }

        var plantedC4 = Utilities.CreateEntityByName<CPlantedC4>("planted_c4");

        if (plantedC4 == null)
        {
            throw new Exception("c4 is null");
        }

        if (plantedC4.AbsOrigin == null)
        {
            throw new Exception("c4.AbsOrigin is null");
        }

        plantedC4.AbsOrigin.X = playerPawn.AbsOrigin.X;
        plantedC4.AbsOrigin.Y = playerPawn.AbsOrigin.Y;
        plantedC4.AbsOrigin.Z = playerPawn.AbsOrigin.Z;
        plantedC4.HasExploded = false;

        plantedC4.BombSite = (int)bombsite;
        plantedC4.BombTicking = true;
        plantedC4.CannotBeDefused = false;

        plantedC4.DispatchSpawn();

        var gameRules = GetGameRules();
        gameRules.BombPlanted = true;
        gameRules.BombDefused = false;

        SendBombPlantedEvent(player, bombsite);
    }

    private static void SendBombPlantedEvent(CCSPlayerController bombCarrier, Bombsite bombsite)
    {
        if (!bombCarrier.IsValid || bombCarrier.PlayerPawn.Value == null)
        {
            return;
        }

        var eventPtr = NativeAPI.CreateEvent("bomb_planted", true);
        NativeAPI.SetEventPlayerController(eventPtr, "userid", bombCarrier.Handle);
        NativeAPI.SetEventInt(eventPtr, "userid", (int)bombCarrier.PlayerPawn.Value.Index);
        NativeAPI.SetEventInt(eventPtr, "site", (int)bombsite);

        NativeAPI.FireEvent(eventPtr, false);
    }

    public static int ShowSpawns(List<Spawn> spawns, Bombsite? bombsite)
    {
        if (bombsite == null)
        {
            return -1;
        }

        spawns = spawns.Where(spawn => spawn.Bombsite == bombsite).ToList();

        foreach (var spawn in spawns)
        {
            ShowSpawn(spawn);
        }

        Server.PrintToChatAll($"{RetakesPlugin.MessagePrefix}Showing {spawns.Count} spawns for bombsite {bombsite}.");
        return spawns.Count;
    }

    public static void ShowSpawn(Spawn spawn)
    {
        var beam = Utilities.CreateEntityByName<CBeam>("beam") ?? throw new Exception("Failed to create beam entity.");
        beam.StartFrame = 0;
        beam.FrameRate = 0;
        beam.LifeState = 1;
        beam.Width = 5;
        beam.EndWidth = 5;
        beam.Amplitude = 0;
        beam.Speed = 50;
        beam.Flags = 0;
        beam.BeamType = BeamType_t.BEAM_HOSE;
        beam.FadeLength = 10.0f;

        var color = spawn.Team == CsTeam.Terrorist ? (spawn.CanBePlanter ? Color.Orange : Color.Red) : Color.Blue;
        beam.Render = Color.FromArgb(255, color);

        beam.EndPos.X = spawn.Vector.X;
        beam.EndPos.Y = spawn.Vector.Y;
        beam.EndPos.Z = spawn.Vector.Z + 100.0f;

        beam.Teleport(spawn.Vector, new QAngle(IntPtr.Zero), new Vector(IntPtr.Zero, IntPtr.Zero, IntPtr.Zero));
        beam.DispatchSpawn();
    }
}
