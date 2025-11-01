using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using System.Drawing;

using RetakesPlugin.Models;
using RetakesPlugin.Utils;
using RetakesPluginShared.Enums;

namespace RetakesPlugin.Services;

public static class SpawnService
{
    public static int ShowSpawns(RetakesPlugin plugin, List<Spawn> spawns, Bombsite? bombsite)
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

        Server.PrintToChatAll($"{plugin.Localizer["retakes.prefix"]} Showing {spawns.Count} spawns for bombsite {bombsite}.");
        Logger.LogInfo("SpawnService", $"Showing {spawns.Count} spawns for bombsite {bombsite}");

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

    public static void RemoveSpawnBeam(Spawn spawn)
    {
        var beamEntities = Utilities.FindAllEntitiesByDesignerName<CBeam>("beam");

        foreach (var beamEntity in beamEntities)
        {
            if (beamEntity.AbsOrigin == null)
            {
                continue;
            }

            if (beamEntity.AbsOrigin.Z - spawn.Vector.Z == 0 && beamEntity.AbsOrigin.X - spawn.Vector.X == 0 && beamEntity.AbsOrigin.Y - spawn.Vector.Y == 0)
            {
                beamEntity.Remove();
                Logger.LogDebug("SpawnService", "Removed spawn beam");
            }
        }
    }
}