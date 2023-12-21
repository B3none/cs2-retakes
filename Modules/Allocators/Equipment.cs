using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Utils;

namespace RetakesPlugin.Modules.Allocators;

public abstract class Equipment
{
    public static void Allocate(CCSPlayerController player, bool isPlanter = false)
    {
        if (player.TeamNum == (int)CsTeam.Terrorist)
        {
            player.GiveNamedItem(CsItem.AssaultSuit);

            if (isPlanter)
            {
                player.GiveNamedItem(CsItem.Bomb);
            }
        }

        if (player.TeamNum == (int)CsTeam.CounterTerrorist)
        {
            player.GiveNamedItem(CsItem.AssaultSuit);

            if (player.PlayerPawn.Value?.ItemServices?.Handle != null)
            {
                var itemServices = new CCSPlayer_ItemServices(player.PlayerPawn.Value.ItemServices.Handle);
                itemServices.HasDefuser = true;
            }
        }
    }
}