using System;
using Nuclei.Features;

namespace Nuclei.Events;

/// <summary>
///     Declares server-related events.
/// </summary>
public static class ServerEvents
{
    internal static void OnServerStarted()
    {
        TimeService.Initialize();
    }

    /// <summary>
    ///     Event handler for when the server stops.
    /// </summary>
    public static event Action? ServerStopped;
    
    internal static void OnServerStopped()
    {
        ServerStopped?.Invoke();
    }
}
