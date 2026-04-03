using System;
using System.Collections.Specialized;
using System.Net;
using System.Text.RegularExpressions;
using Dapper;
using Npgsql;
using NuclearOption.DedicatedServer.Commands;
using NuclearOption.Networking;
using Nuclei.Features;
using Nuclei.Helpers;

namespace Nuclei.CritzOS.Features;
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

internal class CritzOSDB
{
    private static NpgsqlConnection connection;

    static CritzOSDB()
    {
        connection =
            new NpgsqlConnection(CritzOSGlobals.connectionString);
        connection.Open();
    }

    public static void LogChat(Player player, string message)
    {
        connection.Query($"INSERT INTO chat_log (steamid, message, server_name) VALUES ({player.SteamID}, '{message}', '{CritzOSGlobals.ServerName}');");
    }
    
    public static bool DetermineKick(Player player)
    {
        var minTime = DateTime.SpecifyKind(DateTime.Now.AddDays(-1), DateTimeKind.Utc).ToString("yyyy-MM-dd");
        var teamkill_log_query = connection
            .Query($"SELECT * FROM teamkill_log WHERE steamid = {player.SteamID} AND time >= '{minTime}';").AsList();

        var teamkill_ai_log_query = connection
            .Query($"SELECT * FROM teamkill_ai_log WHERE steamid = {player.SteamID} AND time >= '{minTime}';").AsList();

        if (teamkill_log_query.Count >= 3 ||
            teamkill_ai_log_query.Count >= 15)
        {
            CommandMessage msg = new CommandMessage();
            msg.name = "kick-player";
            msg.arguments = new string[]
            {
                Convert.ToString(player.SteamID)
            };

            if (ServerRemoteCommands.Instance.FindAndRunCommand(msg).StatusCode == StatusCode.Success)
            {
                ReportCommandService.SendReport($"CritzOS {CritzOSGlobals.ServerName}", $"Player {player.PlayerName} has been autokicked");
                connection.Query($"INSERT INTO kick_log (steamid) VALUES ({player.SteamID});");
            }
        }

        return false;
    }

    // Determines to ban based on # of kicks at a given time
    public static bool DetermineBan(Player player)
    {
        var minTime = DateTime.SpecifyKind(DateTime.Now.AddDays(-14), DateTimeKind.Utc).ToString("yyyy-MM-dd");
        
        var kick_log_query = connection.Query($"SELECT * FROM kick_log WHERE steamid = {player.SteamID} AND time >= '{minTime}';").AsList();
        
        //var votekick_query = connection.Query($"SELECT * FROM votekick_log WHERE time >= {minTime}").AsList();

        if (kick_log_query.Count >= 3)
        {
            CommandMessage msg = new CommandMessage();
            msg.name = "banlist-add";
            msg.arguments = new string[]
            {
                Convert.ToString(player.SteamID), $"Autobanned at {DateTime.Now.ToString("yyyy-MM-dd")}"
            };

            if (ServerRemoteCommands.Instance.FindAndRunCommand(msg).StatusCode == StatusCode.Success)
            {
                ReportCommandService.SendReport($"CritzOS {CritzOSGlobals.ServerName}", $"Player {player.PlayerName} has been autobanned");
            }

            return true;
        }

        return false;
    }

    public static void AddPlayer(ulong steamid, string username)
    {
        // Will safely error out when there's a duplicate
        try
        {
            connection.Query($"INSERT INTO players (steamid, username) VALUES ({steamid}, '{PlayerUtils.StripStaffPrefix(username)}');");
        }
        catch
        {
            Nuclei.Logger?.LogInfo($"Player {username} already exists in database");
            
            // Change username to most recent one
            var x = connection.QueryFirst($"SELECT * FROM players WHERE steamid={steamid};");
            if (x.username != username)
                connection.Query($"UPDATE players SET username='{PlayerUtils.StripStaffPrefix(username)}' WHERE steamid={steamid};");
            Nuclei.Logger?.LogInfo($"Updated username {username} in DB");
        }
    }

    public static void logPlayerTeamkill(Player atkPlayer, Player victimPlayer)
    {
        ChatService.SendPrivateChatMessage($"WARNING: TEAMKILLING WILL RESULT IN A KICK OR BAN. BE CAREFUL NEXT TIME!", atkPlayer);
        AddPlayer(atkPlayer.SteamID, atkPlayer.PlayerName);
        AddPlayer(victimPlayer.SteamID, victimPlayer.PlayerName);
        
        connection.Query($"INSERT INTO teamkill_log (steamid, steamidofplayerkilled) VALUES ({atkPlayer.SteamID}, {victimPlayer.SteamID});").AsList();
        
        DetermineKick(atkPlayer);
        DetermineBan(atkPlayer);

    }
    
    public static void logAITeamkill(Player atkPlayer, PersistentUnit victimPU)
    {
        ChatService.SendPrivateChatMessage($"WARNING: TEAMKILLING WILL RESULT IN A KICK OR BAN. BE CAREFUL NEXT TIME!", atkPlayer);
        AddPlayer(atkPlayer.SteamID, atkPlayer.PlayerName);
        
        connection.Query($"INSERT INTO teamkill_ai_log (steamid, aitype) VALUES ({atkPlayer.SteamID}, '{victimPU.unitName}');").AsList();
        
        DetermineKick(atkPlayer);
        DetermineBan(atkPlayer);
    }

    public static void LogVoteKick(Player targetPlayer, Player initiator)
    {
        AddPlayer(targetPlayer.SteamID, targetPlayer.PlayerName);
        AddPlayer(initiator.SteamID, initiator.PlayerName);
        connection.Query($"INSERT INTO votekick_log (steamid, steamid_of_votekick_initiator) VALUES ({targetPlayer.SteamID}, {initiator.SteamID});").AsList();
    }
}

public class ReportCommandService
{
    internal static string webhookURL = Environment.GetEnvironmentVariable("webhookURL")!;
    internal static string chatLogWebhookURL = Environment.GetEnvironmentVariable("chatLogWebhookURL")!;
    public static bool SendReport(string username, string message)
    {
        return SendDiscordMessage(username, message, webhookURL);
    }

    public static bool LogChatMessage(string username, string message)
    {
        return SendDiscordMessage(username, message, chatLogWebhookURL);
    }

    private static bool SendDiscordMessage(string username, string message, string url)
    {
        NameValueCollection discordValues = new NameValueCollection();
        discordValues.Add("username", username);
        //discordValues.Add("avatar_url", profilepic);
        discordValues.Add("content", Regex.Replace(message, @"@", ""));

        try
        {
            new WebClient().UploadValues(url, discordValues);
            return true;
        }
        catch (WebException e)
        {
            Nuclei.Logger?.LogError(e.Message);
            return false;
        }
        
    }
}
public class Players
{
    public ulong steamid { get; set; }
    public string username { get; set; }
}

public class AITeamkillLog
{
    public ulong steamid { get; set; }
    public string unitType { get; set; }
}

public class PlayerTeamkillLog
{
    public ulong steamid { get; set; }
    public ulong victim { get; set; }
}