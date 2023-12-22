using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;

namespace RetakesPlugin.Modules;

public static class Helpers
{
    private static readonly Random Random = new();
    
    public static bool IsValidPlayer(CCSPlayerController? player)
    {
        return player != null && player.IsValid;
    }
    
    public static bool DoesPlayerHavePawn(CCSPlayerController? player)
    {
        if (!IsValidPlayer(player))
        {
            return false;
        }
        
        var playerPawn = player!.PlayerPawn.Value;
        
        // Beware, this is also checking if they're alive.
        return playerPawn != null
               && playerPawn is { Health: > 0, AbsOrigin: not null, AbsRotation: not null };
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
        List<T> shuffledList = new List<T>(list); // Create a copy of the original list

        int n = shuffledList.Count;
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
    
    public static void RemoveAllItemsAndEntities(CCSPlayerController player)
    {
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
    
    public static int GetCurrentNumTerrorists()
    {
        Console.WriteLine($"{RetakesPlugin.MessagePrefix} GetCurrentNumTerrorists called");
        // var gameRules = GetGameRules();
        //
        // if (gameRules != null)
        // {
        //     return gameRules.NumTerrorist;
        // }
        
        var numTerrorists = 0;

        foreach (var player in Utilities.GetPlayers().Where(player => IsValidPlayer(player) && IsPlayerConnected(player)))
        {
            if (player.TeamNum == (int)CsTeam.Terrorist)
            {
                Console.WriteLine($"{RetakesPlugin.MessagePrefix} Found terrorist! {player.PlayerName}");
                numTerrorists++;
            }
        }

        return numTerrorists;
    }
}
