using NuclearOption.Networking;
using Nuclei.Features;
using Nuclei.Helpers;
using UnityEngine;

namespace Nuclei.Plugins;
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

public static class RankCatchUpService
{
    public static void CatchUpPlayer(Player player)
    {
        if (player.GetAuthData().SaveData.Faction != null)
        {
            return; // Means that they already joined the server. No double-dipping!
        }
        var currentMissionTime = Time.timeSinceLevelLoad;
        var maxMissionTime = Globals.DedicatedServerManagerInstance.CurrentMissionOption.MaxTime;
        var percentComplete = (currentMissionTime / maxMissionTime) * 2;

        //int avgRank = (int)Globals.AuthenticatedPlayers.Select(x => x.GetPlayer()!.PlayerRank).Average();

        var rank = 0;

        if (percentComplete < .20) return;
        if (percentComplete >= .80)
        {
            rank = 5;
        }
        else if (percentComplete >= .60) 
        {
            rank = 4;
        }
        else if (percentComplete >= .40) 
        {
            rank = 3;
        }
        else if (percentComplete >= .40) 
        {
            rank = 2;
        }
        else if (percentComplete >= .20) 
        {
            rank = 1;
        }

        if (player.PlayerRank > rank) return;
        player.SetRank(rank, false);
        ChatService.SendPrivateChatMessage($"Late join - You have been promoted to Rank {rank}! :)", player);
    }
}