using System;
using System.Collections.Specialized;
using System.Net;
using System.Text.RegularExpressions;
using Dapper;
using Npgsql;
using NuclearOption.DedicatedServer.Commands;
using NuclearOption.Networking;
using NuclearOption.SavedMission;
using Nuclei.Features;
using Nuclei.Helpers;

namespace Nuclei.CritzOS.Features;
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

// ReSharper disable once InconsistentNaming
internal static class CritzOSDB
{
    private static readonly NpgsqlConnection Connection;

    static CritzOSDB()
    {
        Connection =
            new NpgsqlConnection(CritzOSGlobals.ConnectionString);
        Connection.Open();
    }

    public static void LogChat(Player player, string message)
    {
        const string sql = "INSERT INTO chat_log (steamid, message, server_name) VALUES (@steamid, @message, @server_name);";
        Connection.ExecuteAsync(sql, new { steamid = (decimal) player.SteamID, message, server_name = CritzOSGlobals.ServerName });
    }

    // Logs manual kicks
    public static void LogKick(Player player)
    {
        const string sql = "INSERT INTO kick_log (steamid) VALUES (@steamid);";
        Connection.ExecuteAsync(sql, new { steamid = (decimal) player.SteamID });
    }
    
    private static void DetermineKick(Player player)
    {
        var minTime = DateTime.SpecifyKind(DateTime.Now.AddHours(-1), DateTimeKind.Utc).ToString("yyyy-MM-dd");
        var teamkillLogQuery = Connection
            .Query($"SELECT * FROM teamkill_log WHERE steamid = {(decimal) player.SteamID} AND time >= '{minTime}';").AsList();

        var teamkillAILogQuery = Connection
            .Query($"SELECT * FROM teamkill_ai_log WHERE steamid = {(decimal) player.SteamID} AND time >= '{minTime}';").AsList();
        
        var kickLogQuery = Connection
            .Query($"SELECT * FROM kick_log WHERE steamid = {(decimal) player.SteamID} AND time >= '{minTime}';").AsList();

        if (teamkillLogQuery.Count / (kickLogQuery.Count + 1) >= 4 ||
            teamkillAILogQuery.Count / (kickLogQuery.Count + 1) >= 20)
        {
            CommandMessage msg = new CommandMessage
            {
                name = "kick-player",
                arguments =
                [
                    Convert.ToString((decimal) player.SteamID)
                ]
            };

            if (ServerRemoteCommands.Instance.FindAndRunCommand(msg).StatusCode == StatusCode.Success)
            {
                ReportCommandService.SendReport($"CritzOS {CritzOSGlobals.ServerName}", $"Player {player.PlayerName} has been autokicked");
                LogKick(player);
            }
        }
    }

    
    // Review based on # of kicks within a span of time
    private static void IsMarkedForReview(Player player)
    {
        var minTime = DateTime.SpecifyKind(DateTime.Now.AddDays(-14), DateTimeKind.Utc).ToString("yyyy-MM-dd");
        var kickLogQuery = Connection.Query($"SELECT * FROM kick_log WHERE steamid = {player.SteamID} AND time >= '{minTime}';").AsList();

        if (kickLogQuery.Count >= 3)
        {
            ReportCommandService.SendReportUnsanitized($"CritzOS {CritzOSGlobals.ServerName}",
                $"<@&1489759287936024726> Player {player.PlayerName} (||{player.SteamID}||) has been been marked for review");
        }

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
            Connection.ExecuteAsync(sql, new { steamid = (decimal)  playerSteamID, username = playerUsername });
        }
        catch
        {
            Nuclei.Logger?.LogInfo($"Player {playerUsername} already exists in database");
            
            // Change username to the most recent one
            var x = Connection.QueryFirst<Players>($"SELECT username FROM players WHERE steamid = @steamid;", new {steamid = (decimal) playerSteamID});
            if (x.username != PlayerUtils.StripAllPrefix(playerUsername))
            {
                var sql =
                    "UPDATE players SET username = @username WHERE steamid = @steamid;";
                Connection.ExecuteAsync(sql, new {username = PlayerUtils.StripAllPrefix(playerUsername), steamid = (decimal) playerSteamID});
                Nuclei.Logger?.LogInfo($"Updated username {playerUsername} in DB");
            }
        }
    }

    public static void LogVoteskipSuccess(ulong steamid, Mission currentMission)
    {
        var currentMissionName = currentMission.Name;
        var currentTime = MissionService.GetCurrentMissionTime();
        
        var sql = "INSERT INTO voteskip_log (steamid, mission_name, mission_time_at_skip) VALUES ( @steamid, @mission_name, @mission_time_at_skip );";
        Connection.ExecuteAsync(sql,
            new
            {
                steamid = (decimal) steamid, 
                mission_name = currentMissionName,
                mission_time_at_skip = currentTime
            });
    }

    public static void LogPlayerTeamkill(Player atkPlayer, Player victimPlayer)
    {
        ChatService.SendPrivateChatMessage($"WARNING: TEAMKILLING WILL RESULT IN A KICK OR BAN. BE CAREFUL NEXT TIME!", atkPlayer);
        
        const string sql = "INSERT INTO teamkill_log (steamid, steamidofplayerkilled, attacker_aircraft_type, victim_aircraft_type) VALUES (@steamid, @steamidofplayerkilled, @attacker_aircraft_type, @victim_aircraft_type );";
        Connection.ExecuteAsync(sql,
            new
            {
                steamid = (decimal) atkPlayer.SteamID, 
                steamidofplayerkilled = victimPlayer.SteamID,
                attacker_aircraft_type = atkPlayer.Aircraft.unitName,
                victim_aircraft_type = victimPlayer.Aircraft.unitName
            });

        DetermineKick(atkPlayer);
        IsMarkedForReview(atkPlayer);

    }
    
    // ReSharper disable once InconsistentNaming
    public static void LogAITeamkill(Player atkPlayer, PersistentUnit victimPU)
    {
        ChatService.SendPrivateChatMessage($"WARNING: TEAMKILLING WILL RESULT IN A KICK OR BAN. BE CAREFUL NEXT TIME!", atkPlayer);

        const string sql = "INSERT INTO teamkill_ai_log (steamid, attacker_aircraft_type, aitype) VALUES (@steamid, @attacker_aircraft_type, @aitype);";
        Connection.ExecuteAsync(sql,
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
        const string sql = "INSERT INTO votekick_log (steamid, steamid_of_votekick_initiator, reason) VALUES (@steamid, @steamid_of_votekick_initiator, @reason);";
        Connection.ExecuteAsync(sql, 
            new
            {
                steamid = (decimal) targetPlayer.SteamID, 
                steamid_of_votekick_initiator = initiator.SteamID, 
                reason
            });
        Connection.Query($"INSERT INTO votekick_log (steamid, steamid_of_votekick_initiator, reason) VALUES ({targetPlayer.SteamID}, {initiator.SteamID}, '{reason}');").AsList();
    }
}

public static class ReportCommandService
{
    
    public static bool SendReportUnsanitized(string username, string message)
    {
        return SendDiscordMessage(username, message, CritzOSGlobals.WebhookURL);
    }
    
    public static bool SendReport(string username, string message)
    {
        message = Regex.Replace(message, @"@", "");
        return SendDiscordMessage(username, message, CritzOSGlobals.WebhookURL);
    }

    public static bool LogChatMessage(string username, string message)
    {
        message = Regex.Replace(message, @"@", "");
        return SendDiscordMessage(username, message, CritzOSGlobals.ChatLogWebhookURL);
    }

    private static bool SendDiscordMessage(string username, string message, string url)
    {
        var discordValues = new NameValueCollection
        {
            { "username", username },
            //discordValues.Add("avatar_url", profilepic);
            { "content", message }
        };

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
    // ReSharper disable once InconsistentNaming
    public ulong steamid { get; set; }
    
    // ReSharper disable once InconsistentNaming
    public string? username { get; set; }
}