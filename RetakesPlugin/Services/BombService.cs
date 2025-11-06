using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;

using RetakesPlugin.Utils;
using RetakesPluginShared.Enums;

namespace RetakesPlugin.Services;

public class BombService
{
    public static void PlantTickingBomb(CCSPlayerController? player, Bombsite bombsite)
    {
        if (player == null || !player.IsValid)
        {
            throw new Exception("Player controller is not valid");
        }

        var playerPawn = player.Pawn.Value;

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

        var gameRules = GameRulesHelper.GetGameRules();
        gameRules.BombPlanted = true;
        gameRules.BombDefused = false;

        SendBombPlantedEvent(player, bombsite);

        Logger.LogDebug("Bomb", $"Bomb planted at {bombsite} by {player.PlayerName}");
    }

    private static void SendBombPlantedEvent(CCSPlayerController bombCarrier, Bombsite bombsite)
    {
        if (!bombCarrier.IsValid || bombCarrier.PlayerPawn.Value == null)
        {
            return;
        }

        var eventPtr = NativeAPI.CreateEvent("bomb_planted", true);
        NativeAPI.SetEventPlayerController(eventPtr, "userid", bombCarrier.Handle);
        NativeAPI.SetEventInt(eventPtr, "site", (int)bombsite);

        NativeAPI.FireEvent(eventPtr, false);
    }
}