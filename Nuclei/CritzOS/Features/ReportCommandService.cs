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
        var sql = "INSERT INTO chat_log (steamid, message, server_name) VALUES (@steamid, @message, @server_name);";
        connection.ExecuteAsync(sql, new { steamid = (decimal) player.SteamID, message, server_name = CritzOSGlobals.ServerName });
    }

    // Logs manual kicks
    public static void LogKick(Player player)
    {
        var sql = "INSERT INTO kick_log (steamid) VALUES (@steamid);";
        connection.ExecuteAsync(sql, new { steamid = (decimal) player.SteamID });
    }
    
    public static bool DetermineKick(Player player)
    {
        var minTime = DateTime.SpecifyKind(DateTime.Now.AddHours(-1), DateTimeKind.Utc).ToString("yyyy-MM-dd");
        var teamkill_log_query = connection
            .Query($"SELECT * FROM teamkill_log WHERE steamid = {(decimal) player.SteamID} AND time >= '{minTime}';").AsList();

        var teamkill_ai_log_query = connection
            .Query($"SELECT * FROM teamkill_ai_log WHERE steamid = {(decimal) player.SteamID} AND time >= '{minTime}';").AsList();
        
        var kick_log_query = connection
            .Query($"SELECT * FROM kick_log WHERE steamid = {(decimal) player.SteamID} AND time >= '{minTime}';").AsList();

        if (teamkill_log_query.Count / (kick_log_query.Count + 1) >= 4 ||
            teamkill_ai_log_query.Count / (kick_log_query.Count + 1) >= 20)
        {
            CommandMessage msg = new CommandMessage();
            msg.name = "kick-player";
            msg.arguments =
            [
                Convert.ToString((decimal) player.SteamID)
            ];

            if (ServerRemoteCommands.Instance.FindAndRunCommand(msg).StatusCode == StatusCode.Success)
            {
                ReportCommandService.SendReport($"CritzOS {CritzOSGlobals.ServerName}", $"Player {player.PlayerName} has been autokicked");
                LogKick(player);
                return true;
            }
        }

        return false;
    }

    
    // Review based on # of kicks within a span of time
    public static bool IsMarkedForReview(Player player)
    {
        var minTime = DateTime.SpecifyKind(DateTime.Now.AddDays(-14), DateTimeKind.Utc).ToString("yyyy-MM-dd");
        var kick_log_query = connection.Query($"SELECT * FROM kick_log WHERE steamid = {player.SteamID} AND time >= '{minTime}';").AsList();

        if (kick_log_query.Count >= 3)
        {
            ReportCommandService.SendReportUnsanitized($"CritzOS {CritzOSGlobals.ServerName}",
                $"<@&1489759287936024726> Player {player.PlayerName} (||{player.SteamID}||) has been been marked for review");
            return true;
        }

        return false;

        // Original code to instead outright ban 
        /*
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
        */
    }

    public static void AddPlayer(ulong playerSteamID, string playerUsername)
    {
        // Will safely error out when there's a duplicate
        try
        {
            var sql = "INSERT INTO players (steamid, username) VALUES (@steamid, @username);";
            connection.ExecuteAsync(sql, new { steamid = (decimal)  playerSteamID, username = playerUsername });
        }
        catch
        {
            Nuclei.Logger?.LogInfo($"Player {playerUsername} already exists in database");
            
            // Change username to the most recent one
            var x = connection.QueryFirst<Players>($"SELECT username FROM players WHERE steamid = @steamid;", new {steamid = (decimal) playerSteamID});
            if (x.username != PlayerUtils.StripAllPrefix(playerUsername))
            {
                var sql =
                    "UPDATE players SET username = @username WHERE steamid = @steamid;";
                connection.ExecuteAsync(sql, new {username = PlayerUtils.StripAllPrefix(playerUsername), steamid = (decimal) playerSteamID});
                Nuclei.Logger?.LogInfo($"Updated username {playerUsername} in DB");
            }
        }
    }

    public static void logPlayerTeamkill(Player atkPlayer, Player victimPlayer)
    {
        ChatService.SendPrivateChatMessage($"WARNING: TEAMKILLING WILL RESULT IN A KICK OR BAN. BE CAREFUL NEXT TIME!", atkPlayer);
        
        var sql = "INSERT INTO teamkill_log (steamid, steamidofplayerkilled, attacker_aircraft_type, victim_aircraft_type) VALUES (@steamid, @steamidofplayerkilled, @attacker_aircraft_type, @victim_aircraft_type );";
        connection.ExecuteAsync(sql,
            new
            {
                steamid = (decimal) atkPlayer.SteamID, 
                steamidofplayerkilled = victimPlayer.SteamID,
                attacker_aircraft_type = atkPlayer.Aircraft.unitName,
                victim_aircraft_type = victimPlayer.Aircraft.UniqueName
            });

        DetermineKick(atkPlayer);
        IsMarkedForReview(atkPlayer);

    }
    
    public static void logAITeamkill(Player atkPlayer, PersistentUnit victimPU)
    {
        ChatService.SendPrivateChatMessage($"WARNING: TEAMKILLING WILL RESULT IN A KICK OR BAN. BE CAREFUL NEXT TIME!", atkPlayer);

        var sql = "INSERT INTO teamkill_ai_log (steamid, attacker_aircraft_type, aitype) VALUES (@steamid, @attacker_aircraft_type, @aitype);";
        connection.ExecuteAsync(sql,
            new
            {
                steamid = (decimal) atkPlayer.SteamID,
                attacker_aircraft_type = atkPlayer.Aircraft.unitName,
                aitype = victimPU.unitName
            });
        
        DetermineKick(atkPlayer);
        IsMarkedForReview(atkPlayer);
    }

    public static void LogVoteKick(Player targetPlayer, Player initiator, string reason)
    {
        AddPlayer(targetPlayer.SteamID, targetPlayer.PlayerName);
        AddPlayer(initiator.SteamID, initiator.PlayerName);
        var sql = "INSERT INTO votekick_log (steamid, steamid_of_votekick_initiator, reason) VALUES (@steamid, @steamid_of_votekick_initiator, @reason);";
        connection.ExecuteAsync(sql, 
            new
            {
                steamid = (decimal) targetPlayer.SteamID, 
                steamid_of_votekick_initiator = initiator.SteamID, 
                reason
            });
        connection.Query($"INSERT INTO votekick_log (steamid, steamid_of_votekick_initiator, reason) VALUES ({targetPlayer.SteamID}, {initiator.SteamID}, '{reason}');").AsList();
    }
}

public class ReportCommandService
{
    internal static string webhookURL = Environment.GetEnvironmentVariable("webhookURL")!;
    internal static string chatLogWebhookURL = Environment.GetEnvironmentVariable("chatLogWebhookURL")!;
    
    public static bool SendReportUnsanitized(string username, string message)
    {
        return SendDiscordMessage(username, message, webhookURL);
    }
    
    public static bool SendReport(string username, string message)
    {
        message = Regex.Replace(message, @"@", "");
        return SendDiscordMessage(username, message, webhookURL);
    }

    public static bool LogChatMessage(string username, string message)
    {
        message = Regex.Replace(message, @"@", "");
        return SendDiscordMessage(username, message, chatLogWebhookURL);
    }

    private static bool SendDiscordMessage(string username, string message, string url)
    {
        NameValueCollection discordValues = new NameValueCollection();
        discordValues.Add("username", username);
        //discordValues.Add("avatar_url", profilepic);
        discordValues.Add("content", message);

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