using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Utils;

namespace RetakesPlugin.Modules;

public static class Helpers
{
    private static readonly Random Random = new();
    
    public static bool IsValidPlayer(CCSPlayerController? player)
    {
        return player != null && player.IsValid;
    }
    
    public static bool DoesPlayerHavePawn(CCSPlayerController? player, bool shouldBeAlive = true)
    {
        if (!IsValidPlayer(player))
        {
            return false;
        }
        
        var playerPawn = player!.PlayerPawn.Value;

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
    
    public static T GetAndRemoveRandomItem<T>(List<T> list)
    {
        if (list == null || list.Count == 0)
        {
            throw new ArgumentException("List is null or empty");
        }

        Random random = new Random();
        int randomIndex = random.Next(list.Count);
        T randomItem = list[randomIndex];

        list.RemoveAt(randomIndex);

        return randomItem;
    }

    public static List<T> Shuffle<T>(List<T> list)
    {
        var shuffledList = new List<T>(list); // Create a copy of the original list

        var n = shuffledList.Count;
        while (n > 1)
        {
            n--;
            int k = Random.Next(n + 1);
            T value = shuffledList[k];
            shuffledList[k] = shuffledList[n];
            shuffledList[n] = value;
        }

        return shuffledList;
    }
    
    public static CCSGameRules? GetGameRules()
    {
        var gameRulesEntities = Utilities.FindAllEntitiesByDesignerName<CCSGameRulesProxy>("cs_gamerules");
        return gameRulesEntities.First().GameRules!;
    }
    
    public static void RemoveAllWeaponsAndEntities(CCSPlayerController player)
    {
        if (!IsValidPlayer(player))
        {
            return;
        }
        
        if (player.PlayerPawn.Value == null || player.PlayerPawn.Value.WeaponServices == null)
        {
            return;
        }
        
        foreach(var weapon in player.PlayerPawn.Value.WeaponServices.MyWeapons)
        {
            if (weapon is not { IsValid: true, Value.IsValid: true })
            {
                continue;
            }
        
            player.PlayerPawn.Value.RemovePlayerItem(weapon.Value);
            weapon.Value.Remove();
        }
    }
    
    public static bool IsPlayerConnected(CCSPlayerController player)
    {
        return player.Connected == PlayerConnectedState.PlayerConnected;
    }
    
    public static void ExecuteRetakesConfiguration()
    {
        Server.ExecuteCommand("execifexists cs2-retakes/retakes.cfg");
    }
    
    public static int GetCurrentNumPlayers(CsTeam csTeam)
    {
        var players = 0;

        foreach (var player in Utilities.GetPlayers().Where(player => IsValidPlayer(player) && IsPlayerConnected(player)))
        {
            if (player.TeamNum == (int)csTeam)
            {
                players++;
            }
        }

        return players;
    }

    public static bool HasBomb(CCSPlayerController player)
    {
        if (!IsValidPlayer(player))
        {
            return false;
        }
        
        CHandle<CBasePlayerWeapon>? item = null;
        if (player.PlayerPawn.Value == null || player.PlayerPawn.Value.WeaponServices == null)
        {
            return false;
        }

        foreach (var weapon in player.PlayerPawn.Value.WeaponServices.MyWeapons)
        {
            if (weapon is not { IsValid: true, Value.IsValid: true })
                continue;
            if (weapon.Value.DesignerName != "weapon_c4")
                continue;

            item = weapon;
        }

        return item != null && item.Value != null;
    }

    public static void GiveAndSwitchToBomb(CCSPlayerController player)
    {
        player.GiveNamedItem(CsItem.Bomb);
        NativeAPI.IssueClientCommand((int)player.UserId!, "slot5");
    }
}
