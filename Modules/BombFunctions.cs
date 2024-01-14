using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Memory.DynamicFunctions;

namespace RetakesPlugin.Modules;

public static class BombVirtualFunctions
{
    public static MemoryFunctionVoid<IntPtr, IntPtr, IntPtr> ShootSatchelCharge = new(
        @"\x48\x89\x5C\x24\x08\x48\x89\x6C\x24\x10\x56\x57\x41\x56\x48\x83\xEC\x20\x4C\x8B\xF1\x33\xDB"
    );
    
    public static MemoryFunctionVoid<IntPtr, IntPtr, int> PlantBombLinux = new(@"\x55\x48\x8D\x05\x20\x1C\x97\x00");
    public static MemoryFunctionVoid<IntPtr, IntPtr, IntPtr, int> PlantBombWindows = new(@"\x48\x89\x5C\x24\x10\x48\x89\x74\x24\x18\x55\x57\x41\x54\x41\x56\x41\x57\x48\x8D\x6C\x24\xE0\x48\x81\xEC\x20\x01\x00\x00\x45\x33\xE4\x48\xC7\x45\xE8\xFF\xFF\xFF\xFF\x4C\x8B\xF1\x4C\x89\x64\x24\x78\x48\x8B\x0D\x08\xF6\xED\x00");
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
            BombVirtualFunctions.PlantBombLinux.Invoke(
                playerPawn.AbsOrigin.Handle,
                playerPawn.AbsRotation.Handle,
                0
            );
            
            return;
        }
        
        // BombVirtualFunctions.PlantBombWindows.Invoke(
        //     playerPawn.Handle,
        //     playerPawn.AbsOrigin.Handle,
        //     playerPawn.AbsRotation.Handle,
        //     0
        // );
        
        BombVirtualFunctions.ShootSatchelCharge.Invoke(
            playerPawn.Handle,
            playerPawn.AbsOrigin.Handle,
            playerPawn.AbsRotation.Handle
        );
    }
}
