using CounterStrikeSharp.API.Core;

namespace RetakesPlugin.Modules.Managers;

public class Queue
{
    public List<CCSPlayerController> QueuePlayers = new();
    public List<CCSPlayerController> ActivePlayers = new();


    public int NumTerrorists => ActivePlayers;
}