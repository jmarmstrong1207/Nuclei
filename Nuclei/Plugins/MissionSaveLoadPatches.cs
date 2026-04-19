using System;
using BepInEx.Logging;
using HarmonyLib;
using NuclearOption.SavedMission;
using Nuclei.CritzOS;
using Nuclei.CritzOS.Features;
using Nuclei.Features;
using Nuclei.Helpers;
// ReSharper disable InconsistentNaming
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

[HarmonyPriority(Priority.First)]
[HarmonyWrapSafe]
[HarmonyPatch(typeof(MissionSaveLoad))]
public class MissionSaveLoadPatches
{
    [HarmonyPostfix]
    [HarmonyPatch(nameof(MissionSaveLoad.TryLoad))]
    private static void Postfix(
        MissionKey item,
        ref Mission mission,
        ref string error,
        ref bool __result)
    {
        if (!__result || mission == null) return;

        PlayerUtils.ResetIDCount();
        RandomizeWeather(ref mission);
        ModifyDifficulty(ref mission);
        CancelVote();
        ReportCommandService.LogChatMessage(CritzOSGlobals.ServerName,$"LOADING MISSION {mission.Name}");
        //RandomizeTeam(ref mission);
    }

    private static void CancelVote()
    {
        if (VoteService.ActiveVote != null && VoteService.ActiveVote.CancelIfMissionChanges)
            VoteService.ActiveVote.FinaliseVote(false);
    }

    private static void ModifyDifficulty(ref Mission mission)
    {
        // This is set to scale for larger player counts better
        foreach (var f in mission.factions)
        {
            f.addAIPerEnemyPlayer = 0.80f;
            f.AIAircraftLimit = 8;
        }

        mission.missionSettings.nuclearEscalationThreshold =
            Math.Max(mission.missionSettings.nuclearEscalationThreshold, 1681);

        mission.missionSettings.strategicEscalationThreshold =
            Math.Max(mission.missionSettings.strategicEscalationThreshold, 2500); 
        
    }

    private static void RandomizeWeather(ref Mission mission)
    {
        if (!NucleiConfig.RandomizeWeather!.Value) return;
        
        var rnd = new Random();
        mission.environment.timeOfDay = rnd.Next(3, 18);
        mission.environment.timeFactor = 8f;
        mission.environment.weatherIntensity = (float)(rnd.NextDouble() * 0.9);
        mission.environment.cloudAltitude = (float)(500 + rnd.NextDouble() * 1000);
        mission.environment.windSpeed = (float)(rnd.NextDouble() * 4);
        mission.environment.windTurbulence = (float)(rnd.NextDouble()* 0.8);
        mission.environment.windHeading = rnd.Next(0, 360);
    }
    
    // CRITZOS SPECIFIC! WOULD NEED CONFIG ADDING TO MAKE IT PUBLIC BASICALLY
    private static void RandomizeTeam(ref Mission mission)
    {
        if (mission.Name == "THE BOSCALI INVASION - FALL OF FELDSPAR")
        {
            Nuclei.Nuclei.Logger?.LogInfo("SKIPPING TEAM RANDOMIZATION FOR THIS MISSION");
            return;
        }
        var rnd = new Random();
        int probability = rnd.Next(0, 100);
        if (probability <= 50)
        {
            mission.factions[0].preventJoin = true;
            mission.factions[1].preventJoin = false;
        }
        else
        {
            mission.factions[0].preventJoin = false;
            mission.factions[1].preventJoin = true;
        }
        Nuclei.Nuclei.Logger?.LogInfo($"{mission.factions[0].factionName} preventjoin set to {mission.factions[0].preventJoin}");
        Nuclei.Nuclei.Logger?.LogInfo($"{mission.factions[1].factionName} preventjoin set to {mission.factions[1].preventJoin}");
    }
}