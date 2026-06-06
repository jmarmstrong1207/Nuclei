using System;
using HarmonyLib;
using Mirage;
using Nuclei.CritzOS.Features.IPC;
using Nuclei.Events;
using Nuclei.Helpers;

namespace Nuclei.Patches;

[HarmonyPatch(typeof(NetworkServer))]
[HarmonyPriority(Priority.First)]
[HarmonyWrapSafe]
internal static class NetworkServerPatches
{
    [HarmonyPrefix]
    [HarmonyPatch(nameof(NetworkServer.StartServer))]
    private static void StartServerPrefix(ref NetworkClient localClient)
    {
        localClient.RunInBackground = true;
    }
    
    [HarmonyPostfix]
    [HarmonyPatch(nameof(NetworkServer.StartServer))]
    private static void StartServerPostfix()
    {
        ServerEvents.OnServerStarted();
        try
        {
            var port = Globals.DedicatedServerManagerInstance.Config.QueryPort.Value + 2; // Always 1 increment above this
            Nuclei.Logger.LogInfo($"TCP Port: {port}");
            Nuclei.Instance._socket = new Socket();
            Nuclei.Instance._socket.OnJson += Nuclei.Instance.HandleJson;
            Nuclei.Instance._socket.Start("10.0.0.9", port);

        }
        catch (Exception e)
        {
            Nuclei.Logger.LogError(e);
            throw;
        }
    }
    
    [HarmonyPostfix]
    [HarmonyPatch(nameof(NetworkServer.Stop))]
    private static void StopPostfix()
    {
        ServerEvents.OnServerStopped();
    }

    // TODO: review
    // [HarmonyPrefix]
    // [HarmonyPatch(nameof(NetworkServer.AuthenticationSuccess))]
    // private static void AuthenticationSuccessPrefix(ref INetworkPlayer player, AuthenticationResult result)
    // {
    //     var steamId = player.GetSteamIDUlong();
    //
    //     if (!NucleiConfig.IsBanned(steamId))
    //         return;
    //
    //     Nuclei.Logger?.LogInfo($"Player with Steam ID {steamId} tried to join the game but is banned.");
    //     player.Disconnect();
    // }
}