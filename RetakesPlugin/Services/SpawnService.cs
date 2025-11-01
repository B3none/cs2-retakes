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
    private static readonly List<CBaseEntity> _spawnModels = new();

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
        // Create player model
        var model = Utilities.CreateEntityByName<CDynamicProp>("prop_dynamic");
        if (model == null)
        {
            Logger.LogError("SpawnService", "Failed to create prop_dynamic entity for spawn visualization");
            return;
        }

        try
        {
            // Select model based on team
            string modelPath = spawn.Team == CsTeam.Terrorist ? "characters/models/tm_leet/tm_leet_variantb.vmdl" : "characters/models/ctm_sas/ctm_sas.vmdl";

            model.SetModel(modelPath);
            model.UseAnimGraph = false;
            model.AcceptInput("SetAnimation", value: "tools_preview");
            model.DispatchSpawn();

            // Set color based on team and planter status
            Color teamColor;
            if (spawn.Team == CsTeam.Terrorist)
            {
                teamColor = spawn.CanBePlanter ? Color.Orange : Color.Red;
            }
            else
            {
                teamColor = Color.Blue;
            }

            // Apply color and glow
            model.Render = Color.FromArgb(200, teamColor.R, teamColor.G, teamColor.B);
            model.Glow.GlowColorOverride = teamColor;
            model.Glow.GlowRange = 2000;
            model.Glow.GlowTeam = -1;
            model.Glow.GlowType = 3;
            model.Glow.GlowRangeMin = 25;

            // Position the model
            model.Teleport(spawn.Vector, spawn.QAngle, new Vector(0, 0, 0));

            _spawnModels.Add(model);
            CreateSpawnLabel(spawn);

            Logger.LogDebug("SpawnService", $"Created spawn model for {spawn.Team} at {spawn.Vector}");
        }
        catch (Exception ex)
        {
            Logger.LogError("SpawnService", $"Error creating spawn model: {ex.Message}");
            model?.Remove();
        }
    }

    private static void CreateSpawnLabel(Spawn spawn)
    {
        try
        {
            var spawnText = Utilities.CreateEntityByName<CPointWorldText>("point_worldtext");
            if (spawnText == null) return;

            var teamName = spawn.Team == CsTeam.Terrorist ? "T" : "CT";
            var planterText = spawn.CanBePlanter ? " [PLANTER]" : "";
            var bombsiteText = spawn.Bombsite == Bombsite.A ? "A" : "B";

            spawnText.MessageText = $"{teamName}{planterText}\nBombsite {bombsiteText}\n{spawn.Vector.X:F1} {spawn.Vector.Y:F1} {spawn.Vector.Z:F1}";
            spawnText.Enabled = true;
            spawnText.FontSize = 25f;

            Color textColor;
            if (spawn.Team == CsTeam.Terrorist)
            {
                textColor = spawn.CanBePlanter ? Color.Orange : Color.Red;
            }
            else
            {
                textColor = Color.Blue;
            }

            spawnText.Color = textColor;
            spawnText.Fullbright = true;
            spawnText.WorldUnitsPerPx = 0.1f;
            spawnText.DepthOffset = 0.0f;
            spawnText.JustifyHorizontal = PointWorldTextJustifyHorizontal_t.POINT_WORLD_TEXT_JUSTIFY_HORIZONTAL_CENTER;
            spawnText.JustifyVertical = PointWorldTextJustifyVertical_t.POINT_WORLD_TEXT_JUSTIFY_VERTICAL_CENTER;
            spawnText.ReorientMode = PointWorldTextReorientMode_t.POINT_WORLD_TEXT_REORIENT_NONE;

            var textPos = new Vector(spawn.Vector.X, spawn.Vector.Y, spawn.Vector.Z + 80f);
            var textAngle = new QAngle(spawn.QAngle.X, spawn.QAngle.Y + 90f, spawn.QAngle.Z + 90f);

            spawnText.Teleport(textPos, textAngle);
            spawnText.DispatchSpawn();

            _spawnModels.Add(spawnText);
        }
        catch (Exception ex)
        {
            Logger.LogError("SpawnService", $"Error creating spawn label: {ex.Message}");
        }
    }

    public static void RemoveSpawnBeam(Spawn spawn)
    {
        try
        {
            var modelsToRemove = _spawnModels
                .Where(entity => entity != null && entity.IsValid && IsEntityAtPosition(entity, spawn.Vector))
                .ToList();

            foreach (var model in modelsToRemove)
            {
                model.Remove();
                _spawnModels.Remove(model);
            }

            if (modelsToRemove.Any())
            {
                Logger.LogDebug("SpawnService", $"Removed {modelsToRemove.Count} spawn visualization entities");
            }
        }
        catch (Exception ex)
        {
            Logger.LogError("SpawnService", $"Error removing spawn visualization: {ex.Message}");
        }
    }

    private static bool IsEntityAtPosition(CBaseEntity entity, Vector position)
    {
        if (entity.AbsOrigin == null) return false;

        var distance = Math.Sqrt(
            Math.Pow(entity.AbsOrigin.X - position.X, 2) +
            Math.Pow(entity.AbsOrigin.Y - position.Y, 2) +
            Math.Pow(entity.AbsOrigin.Z - position.Z, 2)
        );

        return distance < 50.0; // Within 50 units
    }

    public static void ClearAllSpawnModels()
    {
        try
        {
            int clearedCount = 0;
            foreach (var model in _spawnModels.ToList())
            {
                if (model != null && model.IsValid)
                {
                    model.Remove();
                    clearedCount++;
                }
            }

            _spawnModels.Clear();

            if (clearedCount > 0)
            {
                Logger.LogInfo("SpawnService", $"Cleared {clearedCount} spawn visualization models");
            }
        }
        catch (Exception ex)
        {
            Logger.LogError("SpawnService", $"Error clearing spawn models: {ex.Message}");
        }
    }
}