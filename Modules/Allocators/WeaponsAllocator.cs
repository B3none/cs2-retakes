using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Utils;

namespace RetakesPlugin.Modules.Allocators;

public abstract class WeaponsAllocator
{
    public static void Allocate(CCSPlayerController player)
    {
        if (player.Team == CsTeam.Terrorist)
        {
            player.GiveNamedItem(CsItem.AK47);
            // player.GiveNamedItem(CsItem.Glock);
            player.GiveNamedItem(CsItem.Deagle);
        }
        
        if (player.Team == CsTeam.CounterTerrorist)
        {
            // player.GiveNamedItem(CsItem.M4A4);
            player.GiveNamedItem(CsItem.M4A1S);
            // player.GiveNamedItem(CsItem.USPS);
            player.GiveNamedItem(CsItem.Deagle);
        }

        player.GiveNamedItem(CsItem.Knife);
    }
}