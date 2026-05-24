using System;
using System.Collections.Specialized;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Nuclei.Features;

namespace Nuclei.CritzOS.Features;
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

// ReSharper disable once InconsistentNaming
public static class ReportCommandService
{
    private static readonly WebClient _webClient;

    static ReportCommandService()
    {
        _webClient = new WebClient();
    }
    
    public static void SendReportUnsanitized(string username, string message)
    {
        SendDiscordMessage(username, message, CritzOSGlobals.WebhookURL);
    }
    
    public static void SendReport(string username, string message)
    {
        message = Regex.Replace(message, @"@", "");
        SendDiscordMessage(username, message, CritzOSGlobals.WebhookURL);
    }
    
    public static void SendManualReport(string username, string message)
    {
        message = Regex.Replace(message, @"@", "");
        SendDiscordMessage(username, $"<@&1489759287936024726> {message} \n\n" +
                                            $"*Mission: {MissionService.GetCurrentMission().Name}* \n" +
                                            $"*Server: {CritzOSGlobals.ServerName}*", CritzOSGlobals.ReportsChannelWebhookURL);
    }

    public static void LogChatMessage(string username, string message)
    {
        message = Regex.Replace(message, @"@", "");
        SendDiscordMessage(username, message, CritzOSGlobals.ChatLogWebhookURL);
    }

    private static void SendDiscordMessage(string username, string message, string url)
    {
        var discordValues = new NameValueCollection
        {
            { "username", username },
            //discordValues.Add("avatar_url", profilepic);
            { "content", message }
        };

        try
        {
            new WebClient().UploadValuesAsync(new Uri(url), discordValues);
        }
        catch (WebException e)
        {
            Nuclei.Logger?.LogError(e.Message);
        }
        
    }

    /*
    public static bool SendFeedback(string playerName, int rate)
    {
        var message = $"Rated mission: {rate}/10 \n\n" +
                        $"*Mission: {MissionService.GetCurrentMission().Name}* \n" +
                        $"*Server: {CritzOSGlobals.ServerName}*";
        return SendDiscordMessage(playerName, message, CritzOSGlobals.FeedbackChannelWebhookURL);

    }
    */
}
