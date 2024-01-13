using System.Drawing;
using System.Reflection;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Utils;
using RetakesPlugin.Modules.Configs;
using RetakesPlugin.Modules.Enums;

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
    
    public static CCSPlayerController? GetBombCarrier()
    {
        foreach (var player in Utilities.GetPlayers().Where(IsValidPlayer))
        {
            CHandle<CBasePlayerWeapon>? item = null;
            if (player.PlayerPawn.Value == null || player.PlayerPawn.Value.WeaponServices == null) return null;

            foreach (var weapon in player.PlayerPawn.Value.WeaponServices.MyWeapons)
            {
                if (weapon is not { IsValid: true, Value.IsValid: true })
                    continue;
                if (weapon.Value.DesignerName != "weapon_c4")
                    continue;

                item = weapon;
            }

            if (item != null && item.Value != null)
            {
                return player;
            }
        }

        return null;
    }
    
    public static void DebugObject(string prefix, object myObject, List<string> nestedObjects)
    {
        var myType = myObject.GetType();
        var props = new List<PropertyInfo>(myType.GetProperties());

        foreach (var prop in props)
        {
            try
            {
                object? propValue;
                
                if (nestedObjects.Contains(prop.Name))
                {
                    propValue = prop.GetValue(myObject, null);
                    
                    if (propValue != null)
                    {
                        DebugObject($"{prefix}.{prop.Name}", propValue, nestedObjects);
                    }
                    
                    continue;
                }
                
                propValue = prop.GetValue(myObject, null);

                Console.WriteLine($"{prefix}.{prop.Name} = {propValue ?? "null"}");
            }
            catch (Exception)
            {
                Console.WriteLine($"{prefix}.{prop.Name} = exception");
            }
        }
    }
    
    public static void AcceptInput(IntPtr handle, string inputName, IntPtr activator, IntPtr caller, string value)
    {
        VirtualFunctions.AcceptInput(handle, inputName, activator, caller, value, 0);
    }

    public static CPlantedC4? GetPlantedC4()
    {
        return Utilities.FindAllEntitiesByDesignerName<CPlantedC4>("planted_c4").FirstOrDefault();
    }

    public static bool IsInRange(float range, Vector v1, Vector v2)
    {
        var dx = v1.X - v2.X;
        var dy = v1.Y - v2.Y;
        
        return Math.Sqrt(Math.Pow(dx, 2) + Math.Pow(dy, 2)) <= range;
    }

    public static void SendBombBeginDefuseEvent(CCSPlayerController player)
    {
        if (player.PlayerPawn.Value == null)
        {
            return;
        }

        Console.WriteLine($"{RetakesPlugin.LogPrefix}Creating event");
        var bombPlantedEvent = NativeAPI.CreateEvent("bomb_begindefuse", true);
        Console.WriteLine($"{RetakesPlugin.LogPrefix}Setting player controller handle");
        NativeAPI.SetEventPlayerController(bombPlantedEvent, "userid", player.Handle);
        
        // Console.WriteLine($"{RetakesPlugin.LogPrefix}Setting userid");
        // NativeAPI.SetEventInt(bombPlantedEvent, "userid", (int)player.PlayerPawn.Value.Index);
        
        Console.WriteLine($"{RetakesPlugin.LogPrefix}Setting haskit");
        NativeAPI.SetEventInt(bombPlantedEvent, "haskit", player.PawnHasDefuser ? 1 : 0);
        
        Console.WriteLine($"{RetakesPlugin.LogPrefix}Setting priority");
        NativeAPI.SetEventInt(bombPlantedEvent, "priority", 5);

        NativeAPI.FireEvent(bombPlantedEvent, false);
    }

    public static void SendBombAbortDefuseEvent(CCSPlayerController player)
    {
        if (player.PlayerPawn.Value == null)
        {
            return;
        }

        Console.WriteLine($"{RetakesPlugin.LogPrefix}Creating event");
        var bombPlantedEvent = NativeAPI.CreateEvent("bomb_abortdefuse", true);
        Console.WriteLine($"{RetakesPlugin.LogPrefix}Setting player controller handle");
        NativeAPI.SetEventPlayerController(bombPlantedEvent, "userid", player.Handle);
        
        // Console.WriteLine($"{RetakesPlugin.LogPrefix}Setting userid");
        // NativeAPI.SetEventInt(bombPlantedEvent, "userid", (int)player.PlayerPawn.Value.Index);
        
        Console.WriteLine($"{RetakesPlugin.LogPrefix}Setting site");
        NativeAPI.SetEventInt(bombPlantedEvent, "site", player.PawnHasDefuser ? 1 : 0);
        
        Console.WriteLine($"{RetakesPlugin.LogPrefix}Setting priority");
        NativeAPI.SetEventInt(bombPlantedEvent, "priority", 5);

        NativeAPI.FireEvent(bombPlantedEvent, false);
    }

    public static void SendBombBeginPlantEvent(CCSPlayerController bombCarrier, Bombsite bombsite)
    {
        if (bombCarrier.PlayerPawn.Value == null)
        {
            return;
        }

        Console.WriteLine($"{RetakesPlugin.LogPrefix}Creating event");
        var bombPlantedEvent = NativeAPI.CreateEvent("bomb_beginplant", true);
        Console.WriteLine($"{RetakesPlugin.LogPrefix}Setting player controller handle");
        NativeAPI.SetEventPlayerController(bombPlantedEvent, "userid", bombCarrier.Handle);
        
        // Console.WriteLine($"{RetakesPlugin.LogPrefix}Setting userid");
        // NativeAPI.SetEventInt(bombPlantedEvent, "userid", (int)bombCarrier.PlayerPawn.Value.Index);
        
        Console.WriteLine($"{RetakesPlugin.LogPrefix}Setting site");
        NativeAPI.SetEventInt(bombPlantedEvent, "site", (int)bombsite);
        
        Console.WriteLine($"{RetakesPlugin.LogPrefix}Setting priority");
        NativeAPI.SetEventInt(bombPlantedEvent, "priority", 5);

        NativeAPI.FireEvent(bombPlantedEvent, false);
    }

    public static void SendBombPlantedEvent(CCSPlayerController bombCarrier, CPlantedC4 plantedC4)
    {
        if (bombCarrier.PlayerPawn.Value == null)
        {
            return;
        }

        Console.WriteLine($"{RetakesPlugin.LogPrefix}Creating event");
        var bombPlantedEvent = NativeAPI.CreateEvent("bomb_planted", true);
        Console.WriteLine($"{RetakesPlugin.LogPrefix}Setting player controller handle");
        NativeAPI.SetEventPlayerController(bombPlantedEvent, "userid", bombCarrier.Handle);
        
        // Console.WriteLine($"{RetakesPlugin.LogPrefix}Setting userid");
        // NativeAPI.SetEventInt(bombPlantedEvent, "userid", (int)bombCarrier.PlayerPawn.Value.Index);
        
        Console.WriteLine($"{RetakesPlugin.LogPrefix}Setting posx to {bombCarrier.PlayerPawn.Value.AbsOrigin!.X}");
        NativeAPI.SetEventFloat(bombPlantedEvent, "posx", bombCarrier.PlayerPawn.Value.AbsOrigin!.X);
        
        Console.WriteLine($"{RetakesPlugin.LogPrefix}Setting posy to {bombCarrier.PlayerPawn.Value.AbsOrigin!.Y}");
        NativeAPI.SetEventFloat(bombPlantedEvent, "posy", bombCarrier.PlayerPawn.Value.AbsOrigin!.Y);
        
        Console.WriteLine($"{RetakesPlugin.LogPrefix}Setting site");
        NativeAPI.SetEventInt(bombPlantedEvent, "site", plantedC4.BombSite);
        
        Console.WriteLine($"{RetakesPlugin.LogPrefix}Setting priority");
        NativeAPI.SetEventInt(bombPlantedEvent, "priority", 5);

        NativeAPI.FireEvent(bombPlantedEvent, false);
    }

    public static bool IsOnGround(CCSPlayerController player)
    {
        return (player.PlayerPawn.Value!.Flags & (int)PlayerFlags.FL_ONGROUND) != 0;
    }

    public static bool IsLookingAtBomb(CCSPlayerPawn playerPawn, CPlantedC4 plantedC4)
    {
        return true;
        
        // TODO: Fix these calculations
        // if (playerPawn.AbsOrigin == null || playerPawn.AbsRotation == null)
        // {
        //     return false;
        // }
        //
        // var playerPos = playerPawn.AbsOrigin;
        // var playerRot = playerPawn.AbsRotation;
        //
        // var bombPos = plantedC4.AbsOrigin!;
        //
        // var playerForward = new Vector();
        // playerForward.X = (float)(Math.Cos(playerRot.Y * Math.PI / 180) * Math.Cos(playerRot.X * Math.PI / 180));
        // playerForward.Y = (float)(Math.Sin(playerRot.Y * Math.PI / 180) * Math.Cos(playerRot.X * Math.PI / 180));
        // playerForward.Z = (float)Math.Sin(playerRot.X * Math.PI / 180);
        //
        // var playerToBomb = bombPos - playerPos;
        //
        // var dotProduct = playerForward.X * playerToBomb.X + playerForward.Y * playerToBomb.Y + playerForward.Z * playerToBomb.Z;
        //
        // var playerToBombLength = Math.Sqrt(Math.Pow(playerToBomb.X, 2) + Math.Pow(playerToBomb.Y, 2) + Math.Pow(playerToBomb.Z, 2));
        //
        // var playerForwardLength = Math.Sqrt(Math.Pow(playerForward.X, 2) + Math.Pow(playerForward.Y, 2) + Math.Pow(playerForward.Z, 2));
        //
        // var angle = Math.Acos(dotProduct / (playerToBombLength * playerForwardLength)) * 180 / Math.PI;
        //
        // Console.WriteLine($"{RetakesPlugin.LogPrefix}Is looking at bomb: {angle < 10}");
        // return angle < 10;
    }
}
