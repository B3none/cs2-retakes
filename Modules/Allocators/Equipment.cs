using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;

namespace RetakesPlugin.Modules.Allocators;

public abstract class Equipment
{
    public static void Allocate(CCSPlayerController player, bool isPlanter = false)
    {
        // Weapon allocation logic
        if (player.TeamNum == (byte)CsTeam.Terrorist)
        {
            player.GiveNamedItem("vesthelm");

            if (isPlanter)
            {
                player.GiveNamedItem("weapon_c4");
            }
        }

        if (player.TeamNum == (byte)CsTeam.CounterTerrorist)
        {
            player.GiveNamedItem("vesthelm");
            player.GiveNamedItem("defuser");
        }
    }
}