using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using Mirage;
using NuclearOption.DedicatedServer.Commands;
using NuclearOption.Networking;
using Nuclei.Features;
// ReSharper disable MemberCanBePrivate.Global
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace Nuclei.Helpers;

/// <summary>
///     Helper class for player-related operations.
/// </summary>
public static class PlayerUtils
{

    public static void KickPlayer(Player player)
    {
        try
        {
            Globals.NetworkManagerNuclearOptionInstance.KickPlayerAsync(player);
        }
        catch (Exception e)
        {
            Nuclei.Logger?.LogError(e);
        }
    }
    public static bool BanPlayer(Player targetPlayer, string reason)
    {
        var msg = new CommandMessage
        {
            name = "banlist-add",
            arguments =
            [
                Convert.ToString(targetPlayer.SteamID), reason
            ]
        };

        if (ServerRemoteCommands.Instance.FindAndRunCommand(msg).StatusCode == StatusCode.Success)
        {
            Nuclei.Logger?.LogInfo($"Player {targetPlayer.PlayerName} has been banned");
            return true;
        }
        else
        {
            Nuclei.Logger?.LogError($"An error has occured while attempting to ban. Report this to the server owner");
            return false;
        }
    }

    /// <summary>
    ///     Get the Player object from an INetworkPlayer object, if available.
    /// </summary>
    /// <param name="networkPlayer"> The INetworkPlayer object. </param>
    /// <param name="player"> The Player component, if available. </param>
    /// <returns> The true or false, if player was found or not. </returns>
    public static bool TryGetPlayer(this INetworkPlayer networkPlayer, out Player? player)
    {
        return PlayerHelper.TryGetPlayer(networkPlayer, out player);
    }

    /// <summary>
    ///     Try to find a player by name.
    /// </summary>
    /// <param name="playerName"> The name of the player to find. </param>
    /// <param name="playerObject"> The Player object, if available. </param>
    /// <returns></returns>
    public static bool TryFindPlayer(string playerName, out Player? playerObject)
    {
        var player = Globals.AuthenticatedPlayers.FirstOrDefault(p =>
        {
            if (p.TryGetPlayer(out var po))
            {
                return StripAllPrefix(po!.PlayerName ?? "").ToLower()
                    .StartsWith(StripAllPrefix(playerName).ToLower());
            }

            return false;
        });

        if (player != null)
            return player.TryGetPlayer(out playerObject);

        playerObject = null;
        return false;
    }
    
    /// <summary>
    ///     Utility function to strip a player name of the staff tag, if they have it.
    /// </summary>
    /// <param name="playerName"> The player name. </param>
    /// <returns>Actual playername.</returns>
    public static string StripStaffPrefix(string playerName)
    {
        if (string.IsNullOrEmpty(playerName))
            return playerName;

        var pattern = $@"^\[\d*\]\s*{Regex.Escape(NucleiConfig.StaffPrefix!.Value)}\s*";
        var cleanName = Regex.Replace(playerName, pattern, "", RegexOptions.IgnoreCase);

        return cleanName;
    }
    
    /// <summary>
    ///     Utility function to strip a player name of the ID tag, if they have it.
    /// </summary>
    /// <param name="playerName"> The player name. </param>
    /// <returns>Actual playername.</returns>
    public static string StripIDPrefix(string playerName)
    {
        if (string.IsNullOrEmpty(playerName))
            return playerName;

        var pattern = $@"^\[\d*\]\s*";
        var cleanName = Regex.Replace(playerName, pattern, "", RegexOptions.IgnoreCase);

        return cleanName;
    }
    
    /// <summary>
    ///     Utility function to strip all prefixes
    /// </summary>
    /// <param name="playerName"> The player name. </param>
    /// <returns>Actual playername.</returns>
    public static string StripAllPrefix(string playerName)
    {
        return StripIDPrefix(StripStaffPrefix(playerName));
    }
    

    /// <summary>
    ///     Apply or remove the staff tag based on player permission level.
    /// </summary>
    /// <param name="playerObject"> The Player object. </param>
    /// <returns></returns>
    public static void ApplyOrRemoveStaffTag(Player playerObject)
    {
        if (!NucleiConfig.UseStaffPrefix!.Value || (!NucleiConfig.IsAdmin(playerObject.SteamID) &&
                                                   !NucleiConfig.IsOwner(playerObject.SteamID) &&
                                                   !NucleiConfig.IsModerator(playerObject.SteamID))) return;
        var newName = $"{NucleiConfig.StaffPrefix!.Value} {playerObject.PlayerName}";
        playerObject.PlayerName = newName;
    }

    internal static int ID = 1;
    public static void ApplyID(Player player)
    {
        var newName = $"[{ID++}] {player.PlayerName}";
        player.PlayerName = newName;
    }

    public static void ResetIDCount()
    {
        ID = 1;
    }

    public static bool TryFindPlayerbyID(int i, out Player? player)
    {
        List<Player> playerList = new List<INetworkPlayer>(Globals.AuthenticatedPlayers).Where(ip => ip != null && PlayerHelper.TryGetPlayer(ip, out Player _)).Select(ip =>
        {
            ip.TryGetPlayer(out var p);
            return p;
        }).ToList()!;
        
        var l = playerList.Where(p => p.PlayerName.StartsWith($"[{i}]")).ToList();
        switch (l.Count)
        {
            case 0:
                Nuclei.Logger?.LogError("Player couldn't be found by ID.");
                player = null;
                return false;
            case > 1:
                Nuclei.Logger?.LogError("Not supposed to happen: Player with identical IDs");
                player = null;
                return false;
            default:
                player = l[0];
                return true;
        }
    }

}