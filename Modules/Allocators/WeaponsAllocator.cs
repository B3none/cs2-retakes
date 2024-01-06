using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Utils;

namespace RetakesPlugin.Modules.Allocators;

public abstract class WeaponsAllocator
{
    public static void Allocate(CCSPlayerController player)
    {
        if ((CsTeam)player.TeamNum == CsTeam.Terrorist)
        {
            player.GiveNamedItem(CsItem.AK47);
            // player.GiveNamedItem(CsItem.Glock);
            player.GiveNamedItem(CsItem.Deagle);
        }
        
        if ((CsTeam)player.TeamNum == CsTeam.CounterTerrorist)
        {
            player.GiveNamedItem(CsItem.M4A1S);
            // player.GiveNamedItem(CsItem.USPS);
            player.GiveNamedItem(CsItem.Deagle);
        }

        player.GiveNamedItem(CsItem.Knife);
    }
}