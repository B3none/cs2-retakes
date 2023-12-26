using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Utils;

namespace RetakesPlugin.Modules.Allocators;

public abstract class Equipment
{
    public static void Allocate(CCSPlayerController player)
    {
        player.GiveNamedItem(CsItem.AssaultSuit);

        if (
            player.TeamNum == (int)CsTeam.CounterTerrorist
            && player.PlayerPawn.Value?.ItemServices?.Handle != null
        ) {
            var itemServices = new CCSPlayer_ItemServices(player.PlayerPawn.Value.ItemServices.Handle);
            itemServices.HasDefuser = true;
        }
    }
}