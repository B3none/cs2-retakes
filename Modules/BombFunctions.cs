using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Memory.DynamicFunctions;
using CounterStrikeSharp.API.Modules.Utils;

namespace RetakesPlugin.Modules;

public static class BombVirtualFunctions
{
    public static MemoryFunctionVoid<IntPtr, IntPtr, IntPtr, bool> Init =
        new(@"\x70\x20\x3c\x66\x69\x6c\x65\x69");
}

public static class BombFunctions
{
    public static void Init(CCSPlayerController player, Vector vecStart, Vector vecAngles, bool trainingPlacedByPlayer)
    {
        BombVirtualFunctions.Init.Invoke(player.Handle, vecStart.Handle, vecAngles.Handle, trainingPlacedByPlayer);
    }
}