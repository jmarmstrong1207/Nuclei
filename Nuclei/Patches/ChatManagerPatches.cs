using System;
using HarmonyLib;
using HarmonyLib.Tools;
using Mirage;
using NuclearOption.Chat;
using Nuclei.CritzOS.Features;
using Nuclei.Features;
using Nuclei.Features.Commands;
using Nuclei.Helpers;

namespace Nuclei.Patches;

[HarmonyPatch(typeof(ChatManager))]
[HarmonyPriority(Priority.First)]
[HarmonyWrapSafe]
internal static class ChatManagerPatches
{
    [HarmonyPrefix]
    [HarmonyPatch("UserCode_CmdSendChatMessage_\u002D456754112")]
    private static bool UserCode_CmdSendChatMessagePrefix(string message, bool allChat, INetworkPlayer sender)
    {
        if (!sender.TryGetPlayer(out var player)) 
            Nuclei.Logger?.LogWarning("Player component is null");

        if (message.StartsWith(NucleiConfig.CommandPrefix!.Value) && message.Length > 1)
            if (CommandService.TryExecuteCommand(player!, message.Remove(0, 1)))
                return false;

        var now = DateTime.Now.ToString("MM/dd - H:mm:ss");
        Nuclei.Logger?.LogInfo(allChat
            ? $"[{now}] {player!.PlayerName} sent message: {message}"
            : $"[{now}] {player!.PlayerName} sent message in {player.HQ.faction.factionName} chat: {message}");

        ReportCommandService.LogChatMessage(player!.PlayerName, message);
        CritzOSDB.LogChat(player, message);

        return true;
    }
}