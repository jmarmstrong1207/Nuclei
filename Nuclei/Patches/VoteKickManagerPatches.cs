using System;
using HarmonyLib;
using NuclearOption.Networking;
using Nuclei.CritzOS;
using Nuclei.CritzOS.Features;

// ReSharper disable InconsistentNaming
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace Nuclei.Patches;

[HarmonyPriority(Priority.First)]
[HarmonyWrapSafe]
[HarmonyPatch(typeof(VoteKickManager))]
public class VoteKickManagerPatches
{
    [HarmonyPostfix]
    [HarmonyPatch(nameof(VoteKickManager.ResolveVote), argumentTypes: [typeof(bool)])]
    private static void Postfix(
        bool passed
        )
    {
        if (!passed) return;
        var targetPlayer = VoteKickManager.i.State.TargetName;
        var targetPlayerID = (ulong)VoteKickManager.i.State.TargetID;
        
        Nuclei.Logger?.LogInfo($"Player {targetPlayer} has been votekicked");
        ReportCommandService.SendReport($"({CritzOSGlobals.ServerName}", $"Votekick for {targetPlayer} has passed.");
        _ = CritzOSDB.LogVoteKickAsync(targetPlayerID, 0, null);

    }
}