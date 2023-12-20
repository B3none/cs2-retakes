using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;

namespace RetakesPlugin.Modules;

public class Helpers
{
    private static readonly Random Random = new Random();
    
    public static bool IsValidPlayer(CCSPlayerController? player)
    {
        return player != null && player.IsValid;
    }
    
    public static bool CanPlayerAddSpawn(CCSPlayerController? player)
    {
        if (!IsValidPlayer(player))
        {
            return false;
        }
        
        var playerPawn = player!.PlayerPawn.Value;
        
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
}
