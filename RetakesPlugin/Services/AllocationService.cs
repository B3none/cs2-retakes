using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Utils;

using RetakesPlugin.Utils;

namespace RetakesPlugin.Services;

public class AllocationService
{
    private readonly Random _random;

    public AllocationService(Random random)
    {
        _random = random;
    }

    public void AllocatePlayer(CCSPlayerController player)
    {
        AllocateEquipment(player);
        AllocateWeapons(player);
        AllocateGrenades(player);
    }

    private void AllocateEquipment(CCSPlayerController player)
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

    private void AllocateWeapons(CCSPlayerController player)
    {
        if (player.Team == CsTeam.Terrorist)
        {
            player.GiveNamedItem(CsItem.AK47);
            player.GiveNamedItem(CsItem.Deagle);
        }

        if (player.Team == CsTeam.CounterTerrorist)
        {
            // Easter egg for klippy
            if (player.PlayerName.Trim() == "klip")
            {
                player.GiveNamedItem(CsItem.M4A4);
            }
            else
            {
                player.GiveNamedItem(CsItem.M4A1S);
            }

            player.GiveNamedItem(CsItem.Deagle);
        }

        player.GiveNamedItem(CsItem.Knife);
    }

    private void AllocateGrenades(CCSPlayerController player)
    {
        switch (_random.Next(4))
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