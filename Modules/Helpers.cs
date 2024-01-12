using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Utils;

namespace RetakesPlugin.Modules;

public static class Helpers
{
    internal static readonly Random Random = new();
    
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

        var randomIndex = Random.Next(list.Count);
        var randomItem = list[randomIndex];

        list.RemoveAt(randomIndex);

        return randomItem;
    }

    public static List<T> Shuffle<T>(IEnumerable<T> list)
    {
        var shuffledList = new List<T>(list); // Create a copy of the original list

        var n = shuffledList.Count;
        while (n > 1)
        {
            n--;
            var k = Random.Next(n + 1);
            T value = shuffledList[k];
            shuffledList[k] = shuffledList[n];
            shuffledList[n] = value;
        }

        return shuffledList;
    }
    
    public static CCSGameRules GetGameRules()
    {
        var gameRulesEntities = Utilities.FindAllEntitiesByDesignerName<CCSGameRulesProxy>("cs_gamerules");
        var gameRules = gameRulesEntities.First().GameRules;
        
        if (gameRules == null)
        {
            throw new Exception($"{RetakesPlugin.LogPrefix}Game rules not found!");
        }
        
        return gameRules;
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
        
        foreach (var weapon in player.PlayerPawn.Value.WeaponServices.MyWeapons)
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
            if ((CsTeam)player.TeamNum == csTeam)
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
            {
                continue;
            }

            if (weapon.Value.DesignerName != "weapon_c4")
            {
                continue;
            }

            item = weapon;
        }

        return item != null && item.Value != null;
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
        itemServices.HasHeavyArmor = false;
    }

    public static void RestartGame()
    {
        if (!GetGameRules().WarmupPeriod)
        {
            CheckRoundDone();
        }

        Server.ExecuteCommand("mp_restartgame 1");
    }
    
    public static void MoveBeam(CEnvBeam? laser, Vector start, Vector end)
    {
        if (laser == null)
        {
            return;
        }

        // set pos
        laser.Teleport(start, new QAngle(), new Vector());

        // end pos
        // NOTE: we cant just move the whole vec
        laser.EndPos.X = end.X;
        laser.EndPos.Y = end.Y;
        laser.EndPos.Z = end.Z;

        Utilities.SetStateChanged(laser,"CBeam", "m_vecEndPos");
    }
    
    public static void CheckRoundDone()
    {
        var tHumanCount = GetCurrentNumPlayers(CsTeam.Terrorist);
        var ctHumanCount= GetCurrentNumPlayers(CsTeam.CounterTerrorist);
        
        if (tHumanCount == 0 || ctHumanCount == 0) 
        {
            // TODO: once this stops crashing on windows use it there too
            if (Environment.OSVersion.Platform == PlatformID.Unix)
            {
                GetGameRules().TerminateRound(0.1f, RoundEndReason.TerroristsWin);
            }
            else
            {
                Console.WriteLine($"{RetakesPlugin.LogPrefix}Windows server detected (Can't use TerminateRound)");
            }
        }
    }
}
