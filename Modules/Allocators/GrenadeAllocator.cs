using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Utils;

namespace RetakesPlugin.Modules.Allocators;

public abstract class GrenadeAllocator
{
    public static void Allocate(CCSPlayerController player)
    {
        switch (Helpers.Random.Next(4))
        {
            case 0:
                player.GiveNamedItem(CsItem.SmokeGrenade);    
                break;
            case 1:
                player.GiveNamedItem(CsItem.Flashbang);
                break;
            case 2:
                player.GiveNamedItem(CsItem.HEGrenade);
                break;
            case 3:
                player.GiveNamedItem((CsTeam)player.TeamNum == CsTeam.Terrorist ? CsItem.Molotov : CsItem.Incendiary);
                break;
        }
    }
}