using System;
using HarmonyLib;
using NuclearOption.Networking;
using Nuclei.Events;
using Nuclei.Features;

namespace Nuclei.Patches;

[HarmonyPatch(typeof(MessageManager))]
[HarmonyPriority(Priority.First)]
[HarmonyWrapSafe]
internal static class MessageManagerPatches
{
    [HarmonyPostfix]
    [HarmonyPatch(nameof(MessageManager.JoinMessage))]
    private static void JoinMessagePostfix(Player joinedPlayer)
    {
        var now = DateTime.Now.ToString("MM/dd - H:mm:ss");
        Nuclei.Logger?.LogInfo($"[{now}] {joinedPlayer.PlayerName} (SteamID {joinedPlayer.SteamID}) joined the game");
        ChatService.SendPrivateChatMessage(NucleiConfig.WelcomeMessage!.Value, joinedPlayer);
        
        PlayerEvents.OnPlayerJoined(joinedPlayer);
    }
    
    [HarmonyPostfix]
    [HarmonyPatch(nameof(MessageManager.DisconnectedMessage))]
    private static void DisconnectedMessagePostfix(Player player)
    {
        var now = DateTime.Now.ToString("MM/dd - H:mm:ss");
        Nuclei.Logger?.LogInfo($"[{now}] {player.PlayerName} (SteamID {player.SteamID}) left the game");
        
        PlayerEvents.OnPlayerLeft(player);
    }
}