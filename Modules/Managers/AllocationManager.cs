using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Utils;

namespace RetakesPlugin.Modules.Managers;

public static class AllocationManager
{
    public static void Allocate(CCSPlayerController player)
    {
        AllocateEquipment(player);
        AllocateWeapons(player);
        AllocateGrenades(player);
    }

    private static void AllocateEquipment(CCSPlayerController player)
    {
        player.GiveNamedItem(CsItem.KevlarHelmet);

        if (
            player.Team == CsTeam.CounterTerrorist
            && player.PlayerPawn.IsValid
            && player.PlayerPawn.Value != null
            && player.PlayerPawn.Value.IsValid
            && player.PlayerPawn.Value.ItemServices != null
        )
        {
            var itemServices = new CCSPlayer_ItemServices(player.PlayerPawn.Value.ItemServices.Handle);
            itemServices.HasDefuser = true;
        }
    }

    private static void AllocateWeapons(CCSPlayerController player)
    {
        if (player.Team == CsTeam.Terrorist)
        {
            player.GiveNamedItem(CsItem.AK47);
            // player.GiveNamedItem(CsItem.Glock);
            player.GiveNamedItem(CsItem.Deagle);
        }

        if (player.Team == CsTeam.CounterTerrorist)
        {
            // @klippy
            if (player.PlayerName.Trim() == "klip")
            {
                player.GiveNamedItem(CsItem.M4A4);
            }
            else
            {
                player.GiveNamedItem(CsItem.M4A1S);
            }

            // player.GiveNamedItem(CsItem.USPS);
            player.GiveNamedItem(CsItem.Deagle);
        }

        player.GiveNamedItem(CsItem.Knife);
    }

    private static void AllocateGrenades(CCSPlayerController player)
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
                player.GiveNamedItem(player.Team == CsTeam.Terrorist ? CsItem.Molotov : CsItem.Incendiary);
                break;
        }
    }
}