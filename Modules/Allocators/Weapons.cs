using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;

namespace RetakesPlugin.Modules.Allocators;

public abstract class Weapons
{
    public static void Allocate(CCSPlayerController player)
    {
        // Weapon allocation logic
        if (player.TeamNum == (int)CsTeam.Terrorist)
        {
            player.GiveNamedItem("weapon_ak47");
            player.GiveNamedItem("weapon_glock");
        }
        
        if (player.TeamNum == (int)CsTeam.CounterTerrorist)
        {
            player.GiveNamedItem("weapon_m4a1_silencer");
            player.GiveNamedItem("weapon_usp");
        }

        player.GiveNamedItem("weapon_knife");
    }
}