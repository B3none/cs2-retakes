using CounterStrikeSharp.API.Core;

namespace RetakesPlugin.Modules;

public class Helpers
{
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
}
