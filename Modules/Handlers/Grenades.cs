using CounterStrikeSharp.API.Core;

namespace RetakesPlugin.Modules.Handlers;

public abstract class Grenades
{
    public static void Allocate(CCSPlayerController player)
    {
        // Grenade allocation logic
        switch (new Random().Next(4))
        {
            case 0:
                player.GiveNamedItem("weapon_smokegrenade");    
                break;
            case 1:
                player.GiveNamedItem("weapon_flashbang");
                break;
            case 2:
                player.GiveNamedItem("weapon_hegrenade");
                break;
            case 3:
                player.GiveNamedItem("weapon_molotov");
                break;
        }
    }
}