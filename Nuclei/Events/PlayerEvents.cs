using System;
using NuclearOption.Networking;
using Nuclei.CritzOS;
using Nuclei.CritzOS.Features;
using Nuclei.Features;

namespace Nuclei.Events;

/// <summary>
///     Declares player-related events.
/// </summary>
public static class PlayerEvents
{
    /// <summary>
    ///     Event handler for when a player joins the game.
    /// </summary>
    public static event Action<Player>? PlayerJoined;

    internal static void OnPlayerJoined(Player e)
    {
        PlayerJoined?.Invoke(e);
        if (NucleiConfig.RankCatchUp!.Value) RankCatchUpService.CatchUpPlayer(e);
        ReportCommandService.LogChatMessage($"CritzOS {CritzOSGlobals.ServerName}",
            $"`{e.PlayerName} ({e.SteamID}) joined the game`");
    }

    /// <summary>
    ///     Event handler for when a player leaves the game.
    /// </summary>
    public static event Action<Player>? PlayerLeft;

    internal static void OnPlayerLeft(Player e)
    {
        PlayerLeft?.Invoke(e);
        ReportCommandService.LogChatMessage($"CritzOS {CritzOSGlobals.ServerName}",
            $"`{e.PlayerName} ({e.SteamID}) left the game`");
    }
}