using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;

namespace RetakesPlugin.Modules.Allocators;

public abstract class Equiptment
{
    public static void Allocate(CCSPlayerController player)
    {
        // Weapon allocation logic
        if (player.TeamNum == (byte)CsTeam.Terrorist)
        {
            player.GiveNamedItem("vesthelm");
        }
        
        if (player.TeamNum == (byte)CsTeam.CounterTerrorist)
        {
            player.GiveNamedItem("vesthelm");
            player.GiveNamedItem("defuser");
        }
    }
}