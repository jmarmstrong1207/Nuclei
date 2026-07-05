using System;
using System.Threading.Tasks;
using Dapper;
using Npgsql;
using NuclearOption.DedicatedServer.Commands;
using NuclearOption.Networking;
using NuclearOption.SavedMission;
using Nuclei.Features;
using Nuclei.Helpers;

namespace Nuclei.CritzOS.Features;

#pragma warning disable CS1591
internal static class CritzOSDB
{
    public static async Task LogChatAsync(Player player, string message)
    {
        var connection = new NpgsqlConnection(CritzOSGlobals.ConnectionString);
        const string sql = "INSERT INTO chat_log (steamid, message, server_name) VALUES (@steamid, @message, @server_name);";
        await connection.ExecuteAsync(sql, new { steamid = (decimal) player.SteamID, message, server_name = CritzOSGlobals.ServerName });
    }

    public static async Task LogPlayerCountAsync()
    {
        var connection = new NpgsqlConnection(CritzOSGlobals.ConnectionString);
        var serverName = CritzOSGlobals.ServerName.ToLower();
        var sql = $"INSERT INTO {serverName}_player_count (player_count) VALUES (@count);";
        await connection.ExecuteAsync(sql, new { count = PlayerUtils.GetPlayerCount() });
    }
    
    // Logs manual kicks
    public static async Task LogKickAsync(ulong player, string reason)
    {
        var connection = new NpgsqlConnection(CritzOSGlobals.ConnectionString);
        const string sql = "INSERT INTO kick_log (steamid, reason) VALUES (@steamid, @reason);";
        await connection.ExecuteAsync(sql, new { steamid = (decimal) player, reason});
    }
    
    private static async Task DetermineKickAsync(Player player)
    {
        var connection = new NpgsqlConnection(CritzOSGlobals.ConnectionString);
        var minTime = DateTime.SpecifyKind(DateTime.Now.AddHours(-1), DateTimeKind.Utc).ToString("yyyy-MM-dd");
        var teamkillLogQuery = (await connection
            .QueryAsync($"SELECT * FROM teamkill_log WHERE steamid = {(decimal) player.SteamID} AND time >= '{minTime}';")).AsList();

        var teamkillAILogQuery = (await connection
            .QueryAsync($"SELECT * FROM teamkill_ai_log WHERE steamid = {(decimal) player.SteamID} AND time >= '{minTime}';")).AsList();
        
        var kickLogQuery = (await connection
            .QueryAsync($"SELECT * FROM kick_log WHERE steamid = {(decimal) player.SteamID} AND time >= '{minTime}';")).AsList();

        if (teamkillLogQuery.Count / (kickLogQuery.Count + 1) >= 4 ||
            teamkillAILogQuery.Count / (kickLogQuery.Count + 1) >= 20)
        {
            var recentTime = DateTime.SpecifyKind(DateTime.Now.AddMinutes(-5), DateTimeKind.Utc).ToString("yyyy-MM-dd");
            var recentKickLogQuery = (await connection
                .QueryAsync($"SELECT * FROM kick_log WHERE steamid = {(decimal) player.SteamID} AND time >= '{recentTime}';")).AsList();

            if (recentKickLogQuery.Count > 0) // Don't kick multiple times in a short period, in case it was a nuke or something
                return;
            
            ReportCommandService.SendReport($"CritzOS {CritzOSGlobals.ServerName}", $"Player {player.PlayerName} has been autokicked");
            var reason = "Auto-kick for Teamkills";
            await PlayerUtils.KickPlayerAsync(player, reason);
            await LogKickAsync(player.SteamID, reason);
            /*
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
                await LogKickAsync(player.SteamID, "Auto-kick for Teamkill");
            }
            */
        }
    }

    
    // Review based on # of kicks within a span of time
    private static async Task CheckMarkedForReviewAsync(Player player)
    {
        var connection = new NpgsqlConnection(CritzOSGlobals.ConnectionString);
        var minTime = DateTime.SpecifyKind(DateTime.Now.AddDays(-7), DateTimeKind.Utc).ToString("yyyy-MM-dd");
        var kickLogQuery = (await connection.QueryAsync($"SELECT * FROM kick_log WHERE steamid = { (decimal) player.SteamID} AND time >= '{minTime}';")).AsList();

        if (kickLogQuery.Count >= 3)
        {
            ReportCommandService.SendReportUnsanitized($"CritzOS {CritzOSGlobals.ServerName}",
                $"<@&1489759287936024726> Player {player.PlayerName} (||{player.SteamID}||) has been been marked for review");
        }
    }

    public static async Task AddPlayerAsync(ulong playerSteamID, string playerUsername)
    {
        var connection = new NpgsqlConnection(CritzOSGlobals.ConnectionString);
        // Will safely error out when there's a duplicate
        try
        {
            var sql = "INSERT INTO players (steamid, username) VALUES (@steamid, @username);";
            await connection.ExecuteAsync(sql, new { steamid = (decimal) playerSteamID, username = playerUsername });
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
                await connection.ExecuteAsync(sql, new {username = PlayerUtils.StripAllPrefix(playerUsername), steamid = (decimal) playerSteamID});
                Nuclei.Logger?.LogInfo($"Updated username {playerUsername} in DB");
                
                // TODO - RECORD USERNAME CHANGE IN SEPARATE TABLE
            }
        }
    }

    public static async Task LogVoteskipSuccessAsync(ulong steamid, Mission currentMission)
    {
        var connection = new NpgsqlConnection(CritzOSGlobals.ConnectionString);
        var currentMissionName = currentMission.Name;
        var currentTime = MissionService.GetCurrentMissionTime();
        
        var sql = "INSERT INTO voteskip_log (steamid, mission_name, mission_time_at_skip) VALUES ( @steamid, @mission_name, @mission_time_at_skip );";
        await connection.ExecuteAsync(sql,
            new
            {
                steamid = (decimal) steamid, 
                mission_name = currentMissionName,
                mission_time_at_skip = currentTime
            });
    }

    public static async Task LogPlayerTeamkillAsync(Player atkPlayer, Player victimPlayer)
    {
        var connection = new NpgsqlConnection(CritzOSGlobals.ConnectionString);
        ChatService.SendPrivateChatMessage($"WARNING: TEAMKILLING WILL RESULT IN A KICK OR BAN. BE CAREFUL NEXT TIME!", atkPlayer);
        
        const string sql = "INSERT INTO teamkill_log (steamid, steamidofplayerkilled, attacker_aircraft_type, victim_aircraft_type) VALUES (@steamid, @steamidofplayerkilled, @attacker_aircraft_type, @victim_aircraft_type );";
        await connection.ExecuteAsync(sql,
            new
            {
                steamid = (decimal) atkPlayer.SteamID, 
                steamidofplayerkilled = (decimal) victimPlayer.SteamID,
                attacker_aircraft_type = atkPlayer.Aircraft.unitName,
                victim_aircraft_type = victimPlayer.Aircraft.unitName
            });

        await DetermineKickAsync(atkPlayer);
        await CheckMarkedForReviewAsync(atkPlayer);

    }
    
    // ReSharper disable once InconsistentNaming
    public static async Task LogAITeamkillAsync(Player atkPlayer, PersistentUnit victimPU)
    {
        var connection = new NpgsqlConnection(CritzOSGlobals.ConnectionString);
        ChatService.SendPrivateChatMessage($"WARNING: TEAMKILLING WILL RESULT IN A KICK OR BAN. BE CAREFUL NEXT TIME!", atkPlayer);

        const string sql = "INSERT INTO teamkill_ai_log (steamid, attacker_aircraft_type, aitype) VALUES (@steamid, @attacker_aircraft_type, @aitype);";
        await connection.ExecuteAsync(sql,
            new
            {
                steamid = (decimal) atkPlayer.SteamID,
                attacker_aircraft_type = atkPlayer.Aircraft.unitName,
                aitype = victimPU.unitName
            });
        
        await DetermineKickAsync(atkPlayer);
        await CheckMarkedForReviewAsync(atkPlayer);
    }

    public static async Task LogVoteKickAsync(ulong targetPlayer, ulong initiator, string? reason)
    {
        var connection = new NpgsqlConnection(CritzOSGlobals.ConnectionString);
        const string sql = "INSERT INTO votekick_log (steamid, steamid_of_votekick_initiator, reason) VALUES (@steamid, @steamid_of_votekick_initiator, @reason);";
        await connection.ExecuteAsync(sql, 
            new
            {
                steamid = (decimal) targetPlayer, 
                steamid_of_votekick_initiator = (decimal) initiator, 
                reason
            });
    }

    public static async Task LogWhisperAsync(Player player, Player targetPlayer, string message)
    {
        var connection = new NpgsqlConnection(CritzOSGlobals.ConnectionString);
        const string sql = "INSERT INTO whisper_log (steamid, target_steamid, message) VALUES (@steamid, @target_steamid, @message);";
        await connection.ExecuteAsync(sql,
            new
            {
                steamid = (decimal) player.SteamID, 
                target_steamid = (decimal) targetPlayer.SteamID,
                message
            });
    }
    private class Players
    {
        // ReSharper disable once InconsistentNaming
        public ulong steamid { get; set; }
    
        // ReSharper disable once InconsistentNaming
        public string? username { get; set; }
    }
}

