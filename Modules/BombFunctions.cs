using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Memory.DynamicFunctions;

namespace RetakesPlugin.Modules;

public static class BombVirtualFunctions
{
    public static MemoryFunctionVoid<IntPtr, IntPtr, IntPtr> ShootSatchelCharge = new(
        @"\x48\x89\x5C\x24\x08\x48\x89\x6C\x24\x10\x56\x57\x41\x56\x48\x83\xEC\x20\x4C\x8B\xF1\x33\xDB");
}

public static class BombFunctions
{
    public static CPlantedC4 ShootSatchelCharge(CCSPlayerPawn? playerPawn)
    {
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

        BombVirtualFunctions.ShootSatchelCharge.Invoke(playerPawn.Handle, playerPawn.AbsOrigin.Handle, playerPawn.AbsRotation.Handle);

        var plantedC4 = Helpers.GetPlantedC4();
        
        if (plantedC4 == null)
        {
            throw new Exception("Planted C4 is null");
        }
        
        return plantedC4;
    }
}
