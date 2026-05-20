using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Mirage;
using NuclearOption.DedicatedServer;
using NuclearOption.Networking;
using NuclearOption.SavedMission;
using NuclearOption.SavedMission.ObjectiveV2;
using Nuclei.Enums;
using Nuclei.Helpers;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Nuclei.Features;

/// <summary>
///     Manages missions on the server.
/// </summary>
public static class MissionService
{
    /// <summary>
    ///     The last mission that was started.
    /// </summary>
    private static Mission? LastMission { get; set; }

    /// <summary>
    ///     The preselected mission key.
    /// </summary>
    private static MissionKey? PreselectedMissionKey { get; set; }

    /// <summary>
    ///     The current mission.
    /// </summary>
    public static Mission? CurrentMission => MissionManager.CurrentMission;

    /// <summary>
    ///     The current mission runner.
    /// </summary>
    private static MissionRunner? CurrentMissionRunner => MissionManager.Runner;

    /// <summary>
    ///     The current mission objectives.
    /// </summary>
    public static MissionObjectives? CurrentMissionObjectives => MissionManager.Objectives;

    /// <summary>
    ///     The current mission active objectives.
    /// </summary>
    public static List<Objective> CurrentMissionActiveObjectives => CurrentMissionRunner?.ActiveObjectives ?? [];

    /// <summary>
    ///     The current mission time.
    /// </summary>
    public static float CurrentMissionTime => Globals.MissionManagerInstance.MissionTime;

    /// <summary>
    ///     Gets all Mission Keys as an IEnumerable.
    /// </summary>
    private static IEnumerable<MissionKey> AllMissionKeys => MissionGroup.All.GetMissions();

    /// <summary>
    ///     Gets a mission by its key.
    /// </summary>
    /// <param name="key"> The mission key object of the mission. </param>
    /// <returns> The mission if found, otherwise null. </returns>
    private static Mission? GetMission(MissionKey key)
    {
        return key.TryLoad(out var mission, out var errorString) ? mission : throw new Exception(errorString);
    }

    /// <summary>
    ///     Gets a mission by its mission object.
    /// </summary>
    /// <param name="mission"> The mission object </param>
    /// <returns> The mission if found, otherwise null. </returns>
    private static MissionKey GetMissionKey(Mission mission)
    {
        return AllMissionKeys.First(k => k.Name == mission.Name);
    }

    /// <summary>
    ///     Gets a random mission from the provided list of missions.
    /// </summary>
    /// <param name="missions"> The list of missions to choose from. </param>
    /// <param name="allowRepeat"> Whether to allow the same mission to be returned multiple times in a row. </param>
    /// <returns></returns>
    private static Mission? GetRandomMission(MissionKey[] missions, bool allowRepeat = false)
    {
        if (missions.Length == 0)
        {
            Nuclei.Logger?.LogError("No missions found. This should not happen.");
            return null;
        }
        if (!allowRepeat && missions.Length > 1 && LastMission != null)
            missions = missions.Where(m => m.Name != LastMission.Name).ToArray();

        return GetMission(missions[Random.Range(0, missions.Length)]);
    }

    /// <summary>
    ///     Select the given mission on the server.
    /// </summary>
    /// <param name="mission"> The mission to start. </param>
    /// <param name="checkIfSame"> Whether to check if the mission is the same as the current mission. </param>
    public static void SetMission(Mission mission, bool checkIfSame = false)
    {
        MissionManager.SetMission(mission, checkIfSame);
        LastMission = mission;
        Nuclei.Logger?.LogDebug($"Set mission: {mission.Name}");
    }

    /// <summary>
    /// gets current mission
    /// </summary>
    /// <returns></returns>
    public static Mission GetCurrentMission()
    {
        return MissionManager.CurrentMission;
    }

    /// <summary>
    ///     Select the next mission on the server.
    /// </summary>
    /// <param name="option"> The mission to start. </param>
    public static void SetNextMission(MissionOptions option)
    {
        Globals.DedicatedServerManagerInstance.SetNextMission(option);
    }
    
    /// <summary>
    /// Gets current mission's max time
    /// </summary>
    /// <returns></returns>
    public static float GetCurrentMissionMaxTime()
    {
        return Globals.DedicatedServerManagerInstance.CurrentMissionOption.MaxTime;
    }
    
    /// <summary>
    /// Get current mission time
    /// </summary>
    /// <returns></returns>
    public static float GetCurrentMissionTime()
    {
        return Time.timeSinceLevelLoad;
    }

    /// <summary>
    ///     Set a mission to be preselected for the next mission start.
    /// </summary>
    /// <param name="key"> The mission key to preselect. </param>
    public static void SetPreselectedMission(MissionKey key)
    {
        PreselectedMissionKey = key;
    }

    /// <summary>
    ///     Return preselected mission if it exists, and clear it.
    /// </summary>
    /// <param name="mission"> The mission to return. </param>
    /// <returns> Whether the mission was found. </returns>
    private static bool TryGetConsumePreselectedMission(out Mission? mission)
    {
        if (PreselectedMissionKey == null)
        {
            mission = null;
            return false;
        }
        mission = GetMission(PreselectedMissionKey.Value)!;
        PreselectedMissionKey = null;
        return true;
    }
    
    
    // TODO: Deconstruct this into smaller, reusable functions
    /// <summary>
    ///     Starts the next mission in the mission rotation.
    /// </summary>
    public static async void StartNextMission(Player? player)
    {
        
        try
        {
            var dsm = Globals.DedicatedServerManagerInstance;
            if (dsm == null)
            {
                Nuclei.Logger?.LogWarning("dsm is null");
                return;
            }

            var mr = dsm.missionRotation;
            if (mr == null)
            {
                Nuclei.Logger?.LogWarning("missionRotation is null");
                return;
            }

            var nextOpt = mr.GetNext();
            if (!nextOpt.Key.TryGetKey(out var key))
            {
                Nuclei.Logger?.LogWarning("Error: could not resolve mission key.");
                return;
            }

            if (!MissionSaveLoad.TryLoad(key, out var mission, out var err))
            {
                Nuclei.Logger?.LogWarning($"Load failed: {err}");
                return;
            }

            Nuclei.Logger?.LogInfo($"Loading next mission: {mission?.Name ?? "<unnamed>"}");
            if (player != null) ChatService.SendPrivateChatMessage("Loading next mission...", player);

            // Switch to main thread for Unity scene/lobby ops
            await UniTask.SwitchToMainThread();

            dsm.UpdateLobby(mission, true);
            var ok = await dsm.LoadNext(mission);
            if (!ok)
            {
                if (player != null) Nuclei.Logger?.LogError("Failed to load next mission.");
                return;
            }

            dsm.currentMission = mission;
            dsm.currentMissionOption = nextOpt;
        }
        catch (Exception e)
        {
            Nuclei.Logger?.LogError(e);
            if (player != null) Nuclei.Logger?.LogError("Unexpected error while loading mission.");
        }
    }

    /// <summary>
    /// Get all missions in queue
    /// </summary>
    /// <returns></returns>
    public static List<MissionOptions> GetAllMissions()
    {
        return Globals.DedicatedServerManagerInstance.missionRotation.allMissions;
    }

    internal static void SendMissionReminder()
    {
        var currentMissionTime = Time.timeSinceLevelLoad;
        var maxMissionTime = MissionService.GetCurrentMissionMaxTime();
        ChatService.SendChatMessage($"Remaining mission time: {(int)((maxMissionTime - currentMissionTime)/60)} minutes");
    }

    internal static void SendEndingMissionReminder()
    {
        var currentMissionTime = MissionService.GetCurrentMissionTime();
        var maxMissionTime = MissionService.GetCurrentMissionMaxTime();
        if (maxMissionTime > 0 && maxMissionTime - currentMissionTime < 120)
            ChatService.SendChatMessage($"MISSION ENDING SOON! Remaining mission time: {(int)(maxMissionTime - currentMissionTime)/60} minutes");
    }

    internal static void SetMinimumWage()
    {
        try
        {
            foreach (var faction in FactionRegistry.GetAllHQs())
            {
                var val = PlayerUtils.GetPlayerCount() * faction.regularIncome;
                if (faction.factionFunds < val)
                {
                    faction.SetFunds(val);
                    return;

                }
            }
        }
        catch (Exception e)
        {
            Nuclei.Logger?.LogError(e);
        }
    }
}