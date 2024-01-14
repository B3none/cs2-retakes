using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Memory.DynamicFunctions;

namespace RetakesPlugin.Modules;

public static class BombVirtualFunctions
{
    public static MemoryFunctionVoid<IntPtr, IntPtr, IntPtr> ShootSatchelCharge = new(
        Environment.OSVersion.Platform == PlatformID.Unix
            // TODO: Fix this linux sig, it's scuffed
            ? @"\x55\x48\x89\xe5\x41\x55\x41\x54\x49\x89\xfc\x53\x48\x83\xec\x28\x48\x8d\x05\xb1\xdb\xcc\x00\x66\x0f\xd6\x55\xc0\x66\x0f\xd6\x45\xd0\xf3\x0f\x11\x4d\xd8\xc7\x45\xc0\x00\x00\x00\x00\x48\x8b\x38\xc7\x45\xc8\x00\x00\x00\x00\xe8\x14\x2d\xf5\xff\x84\xc0\x74\x70\x48\x8d\x75\xc0\x4c\x89\xe2\x89\xc3\x48\x8d\x7d\xd0\xe8\x2e\xf4\xff\xff\x83\xf8\xff"
            : @"\x48\x89\x5C\x24\x08\x48\x89\x6C\x24\x10\x56\x57\x41\x56\x48\x83\xEC\x20\x4C\x8B\xF1\x33\xDB"
    );
}

public static class BombFunctions
{
    public static void ShootSatchelCharge(CCSPlayerPawn? playerPawn)
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
        
        Console.WriteLine(Environment.OSVersion.Platform == PlatformID.Unix ? "Linux" : "Windows");
        
        if (Environment.OSVersion.Platform == PlatformID.Unix)
        {
            BombVirtualFunctions.ShootSatchelCharge.Invoke(
                playerPawn.Handle,
                playerPawn.AbsOrigin.Handle,
                playerPawn.AbsRotation.Handle
            );
            
            // var plantedC4 = Helpers.GetPlantedC4();
            // if (plantedC4 == null)
            // {
            //     return;
            // }
            //
            // plantedC4.Teleport(playerPawn.AbsOrigin, playerPawn.AbsRotation, new Vector());
            
            return;
        }
        
        BombVirtualFunctions.ShootSatchelCharge.Invoke(playerPawn.Handle, playerPawn.AbsOrigin.Handle, playerPawn.AbsRotation.Handle);
    }
}
