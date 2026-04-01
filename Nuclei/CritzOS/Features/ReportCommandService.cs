using System;
using System.Collections.Specialized;
using System.Net;
using System.Text.RegularExpressions;
using Dapper;
using Npgsql;
using NuclearOption.Networking;

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

    public static void AddPlayer(ulong steamid, string username)
    {
        // Will safely error out when there's a duplicate
        connection.Query($"INSERT INTO players (steamid, username) VALUES ({steamid}, '{username}')");
    }

    public static void logPlayerTeamkill(Player atkPlayer, Player victimPlayer)
    {
        AddPlayer(atkPlayer.SteamID, atkPlayer.PlayerName);
        AddPlayer(victimPlayer.SteamID, victimPlayer.PlayerName);

        connection.Query($"INSERT INTO teamkill_log (steamid, steamidofplayerkilled) VALUES ({atkPlayer.SteamID}, {victimPlayer.SteamID})").AsList();
    }
    
    public static void logAITeamkill(Player atkPlayer, PersistentUnit victimPU)
    {
        AddPlayer(atkPlayer.SteamID, atkPlayer.PlayerName);

        connection.Query($"INSERT INTO teamkill_ai_log (steamid, steamidofplayerkilled) VALUES ({atkPlayer.SteamID}, '{victimPU.unitName}')").AsList();
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