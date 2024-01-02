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
            && player.PlayerPawn.IsValid
            && player.PlayerPawn.Value != null
            && player.PlayerPawn.Value.IsValid
            && player.PlayerPawn.Value.ItemServices != null
        ) {
            var itemServices = new CCSPlayer_ItemServices(player.PlayerPawn.Value.ItemServices.Handle);
            itemServices.HasDefuser = true;
        }
    }
}