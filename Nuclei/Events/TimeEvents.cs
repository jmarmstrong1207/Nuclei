using System;
using System.Linq;
using Mirage;
using NuclearOption.Networking;
using Nuclei.Features;
using Nuclei.Features.Commands;
using Nuclei.Helpers;
using UnityEngine;

namespace Nuclei.Events;

/// <summary>
///     Time-related events for Nuclei.
/// </summary>
public static class TimeEvents
{
    // every second, every 30 seconds, every minute, every 10 minutes, every 30 minutes, every hour

    internal static void OnEverySecond()
    {
    }

    internal static void OnEvery30Seconds()
    {
    }

    internal static void OnEveryMinute()
    {
        MissionService.SendEndingMissionReminder();
        
        MissionService.SetMinimumWage();
    }

    internal static void OnEvery10Minutes()
    {
        MissionService.SendMissionReminder();

    }

    internal static void OnEvery30Minutes()
    {
    }

    internal static void OnEveryHour()
    {
    }
}