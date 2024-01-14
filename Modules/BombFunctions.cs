using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Memory.DynamicFunctions;
using RetakesPlugin.Modules.Enums;

namespace RetakesPlugin.Modules;

public static class BombVirtualFunctions
{
    public static MemoryFunctionVoid<IntPtr, IntPtr, IntPtr> ShootSatchelChargeWindows = new(
        @"\x48\x89\x5C\x24\x08\x48\x89\x6C\x24\x10\x56\x57\x41\x56\x48\x83\xEC\x20\x4C\x8B\xF1\x33\xDB"
    );
    
    public static MemoryFunctionVoid<IntPtr, IntPtr, int> SpawnBombLinux = new(
        @"\x55\x48\x8D\x05\x20\x1C\x97\x00"
    );
}

public static class BombFunctions
{
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
        
        Console.WriteLine(Environment.OSVersion.Platform == PlatformID.Unix ? "Linux" : "Windows");
        
        if (Environment.OSVersion.Platform == PlatformID.Unix)
        {
            BombVirtualFunctions.SpawnBombLinux.Invoke(
                playerPawn.AbsOrigin.Handle,
                playerPawn.AbsRotation.Handle,
                0
            );

            var plantedC4 = Helpers.GetPlantedC4();

            if (plantedC4 != null)
            {
                Schema.SetSchemaValue(plantedC4.Handle, "CBaseEntity", "m_hOwnerEntity", playerPawn.Handle);
                plantedC4.BombTicking = true;
            }
        }
        else
        {
            BombVirtualFunctions.ShootSatchelChargeWindows.Invoke(
                playerPawn.Handle,
                playerPawn.AbsOrigin.Handle,
                playerPawn.AbsRotation.Handle
            );
        }
        
        // Need to fire bomb planted manually
        Helpers.SendBombPlantedEvent(
            player,
            bombsite
        );
    }
}
