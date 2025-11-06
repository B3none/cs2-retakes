using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Utils;
using System.Diagnostics.CodeAnalysis;

namespace RetakesPlugin.Utils;

public static class PlayerHelper
{
    public static bool IsValid([NotNullWhen(true)] CCSPlayerController? player)
    {
        return player != null && player.IsValid;
    }

    public static bool IsConnected(CCSPlayerController player)
    {
        return player.Connected == PlayerConnectedState.PlayerConnected;
    }

    public static bool HasAlivePawn(CCSPlayerController? player, bool shouldBeAlive = true)
    {
        if (!IsValid(player))
        {
            return false;
        }

        var playerPawn = player!.Pawn.Value;

        if (playerPawn == null || playerPawn is { AbsOrigin: null, AbsRotation: null })
        {
            return false;
        }

        if (shouldBeAlive && !(playerPawn.Health > 0))
        {
            return false;
        }

        return true;
    }

    public static int GetPlayerCount(CsTeam? csTeam = null)
    {
        var players = 0;

        foreach (var player in Utilities.GetPlayers().Where(IsValid))
        {
            if (csTeam == null || player.Team == csTeam)
            {
                players++;
            }
        }

        return players;
    }

    public static void GiveAndSwitchToBomb(CCSPlayerController player)
    {
        player.GiveNamedItem(CsItem.Bomb);
        NativeAPI.IssueClientCommand((int)player.UserId!, "slot5");
    }

    public static void RemoveHelmetAndHeavyArmour(CCSPlayerController player)
    {
        if (player.PlayerPawn.Value == null || player.PlayerPawn.Value.ItemServices == null)
        {
            return;
        }

        var itemServices = new CCSPlayer_ItemServices(player.PlayerPawn.Value.ItemServices.Handle);
        itemServices.HasHelmet = false;
    }

    public static bool HasQueuePriority(CCSPlayerController player, string[] queuePriorityFlags)
    {
        return PlayerHasAnyQueuePermission(player, queuePriorityFlags);
    }

    public static bool HasQueueImmunity(CCSPlayerController player, string[] queueImmunityFlags)
    {
        return PlayerHasAnyQueuePermission(player, queueImmunityFlags);
    }

    public static bool HasAdminPermission(CCSPlayerController? player, params string[] permissionFlags)
    {
        if (player == null)
        {
            return true;
        }

        return PlayerHasAnyQueuePermission(player, permissionFlags);
    }

    private static bool PlayerHasAnyQueuePermission(CCSPlayerController player, IEnumerable<string> permissionFlags)
    {
        foreach (var permissionFlag in permissionFlags)
        {
            if (string.IsNullOrWhiteSpace(permissionFlag))
            {
                continue;
            }

            if (AdminManager.PlayerHasPermissions(player, permissionFlag))
            {
                return true;
            }
        }

        return false;
    }

    public static List<T> Shuffle<T>(IEnumerable<T> list, Random random)
    {
        var shuffledList = new List<T>(list);

        var n = shuffledList.Count;
        while (n > 1)
        {
            n--;
            var k = random.Next(n + 1);
            T value = shuffledList[k];
            shuffledList[k] = shuffledList[n];
            shuffledList[n] = value;
        }

        return shuffledList;
    }
}