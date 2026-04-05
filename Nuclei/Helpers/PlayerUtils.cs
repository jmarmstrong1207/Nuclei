using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Mirage;
using NuclearOption.DedicatedServer.Commands;
using NuclearOption.Networking;
using Nuclei.Features;

namespace Nuclei.Helpers;

/// <summary>
///     Helper class for player-related operations.
/// </summary>
public static class PlayerUtils
{

    public static bool KickPlayer(Player player)
    {
        try
        {
            Globals.NetworkManagerNuclearOptionInstance.KickPlayerAsync(player);
            return true;
        }
        catch (Exception e)
        {
            return false;
        }
    }
    public static bool BanPlayer(Player targetPlayer, string reason)
    {
        CommandMessage msg = new CommandMessage();
        msg.name = "banlist-add";
        msg.arguments = new string[]
        {
            Convert.ToString(targetPlayer.SteamID), reason
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
            return TryGetPlayer(Globals.AuthenticatedPlayers.FirstOrDefault(p =>
            {
                Player po;
                TryGetPlayer(p, out po);
                return StripStaffPrefix(po.PlayerName ?? "").ToLower()
                    .StartsWith(StripStaffPrefix(playerName).ToLower());
            }), out playerObject);
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

    public static bool TryFindPlayerbyID(int i, out Player? player)
    {
        var playerList = new List<INetworkPlayer>(Globals.AuthenticatedPlayers).Where(ip => PlayerHelper.TryGetPlayer(ip, out Player _)).Select(ip =>
        {
            ip.TryGetPlayer(out var p);
            return p;
        }).ToList();
        List<Player> l = playerList.Where(p => p.PlayerName.StartsWith($"[{i}]")).ToList();
        if (l.Count == 0)
        {
            Nuclei.Logger?.LogError("Player couldn't be found by ID.");
            player = null;
            return false;
        }
        if (l.Count > 1)
        {
            Nuclei.Logger?.LogError("Not supposed to happen: Player with identical IDs");
            player = null;
            return false;
        }
        player = l[0];
        return true;
    }

}