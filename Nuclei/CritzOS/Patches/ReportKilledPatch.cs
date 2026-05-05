using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using Nuclei.CritzOS.Features;
// ReSharper disable all

namespace Nuclei.CritzOS.Patches;

/// <summary>
/// Code to detect kills, primarily who TK'ed what/who
/// </summary>
[HarmonyPatch(typeof(Unit), nameof(Unit.ReportKilled))]
[HarmonyPriority(Priority.First)]
[HarmonyWrapSafe]
public static class ReportKilledPatch
{
    /*
    static readonly FieldInfo f_persistentID = AccessTools.Field(typeof(Unit), "persistentID");
    static readonly FieldInfo f_damageCredit = AccessTools.Field(typeof(Unit), "damageCredit");
    
    // ReSharper disable once ArrangeTypeMemberModifiers
    static void Postfix(object __instance)
    {
        var victimPid = (PersistentID)f_persistentID.GetValue(__instance);
        if (victimPid == null || victimPid == PersistentID.None) return;
        if (!UnitRegistry.TryGetPersistentUnit(victimPid, out var victimPU)) return;

        var victimHQ = victimPU.GetHQ();
        var victimPlayer = victimPU.player;
        if (victimPlayer != null)
        {
            // Nuclei.Statistics.IncrementActionStatistic(victimPlayer.SteamID, "deaths", 1.0f);
        }

        // --- Get damageCredit dict ---
        var dc = f_damageCredit.GetValue(__instance) as Dictionary<PersistentID, float>;
        if (dc == null || dc.Count == 0) return;

        // Find top damager (matches game logic: >= wins last)
        float topDamage = float.MinValue;
        PersistentID topPid = PersistentID.None;
        float total = 0f;
        foreach (var kv in dc) total += kv.Value;
        foreach (var kv in dc)
        {
            if (kv.Value >= topDamage)
            {
                topDamage = kv.Value;
                topPid = kv.Key;
            }
        }

        if (topPid == PersistentID.None) return;

        // Resolve top damager persistent unit
        if (!UnitRegistry.TryGetPersistentUnit(topPid, out var atkPU)) return;
        var atkHQ = atkPU.GetHQ();
        var atkPlayer = atkPU.player;
        if (atkPlayer == null) return;

        // PvP Kills
        if (victimPlayer != null)
        {
            if (atkHQ == victimHQ && atkPlayer != victimPlayer) // Teamkill
            {
                ReportCommandService.SendReport($"CritzOS {CritzOSGlobals.ServerName}",
                    $"{atkPlayer.Aircraft.unitName} (||{atkPlayer.SteamID}||) teamkilled player {victimPlayer.Aircraft.unitName}!");
                CritzOSDB.LogPlayerTeamkill(atkPlayer, victimPlayer);
                Nuclei.Logger?.LogInfo($"{atkPlayer.Aircraft.unitName} (||{atkPlayer.SteamID}||) teamkilled player {victimPlayer.Aircraft.unitName}!");
            }
            else
            {
                Nuclei.Logger?.LogInfo($"{atkPlayer.PlayerName} killed {victimPlayer.PlayerName}.");
            }
        }

        // === ANTI AI TEAMKILL ===
        if (atkHQ == victimHQ)
        {
            if (victimPlayer == null)
            {
                ReportCommandService.SendReport($"CritzOS {CritzOSGlobals.ServerName}", $"{atkPlayer.Aircraft.unitName} (||{atkPlayer.SteamID}||) killed friendly {victimPU.unitName}!");
                CritzOSDB.LogAITeamkill(atkPlayer, victimPU);
                Nuclei.Logger?.LogInfo($"{atkPlayer.Aircraft.unitName} (||{atkPlayer.SteamID}||) killed friendly {victimPU.unitName}!");
            }
        }
    }
    */
}