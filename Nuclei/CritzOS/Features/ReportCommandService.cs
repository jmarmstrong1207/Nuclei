using System.Collections.Specialized;
using System.Net;
using System.Text.RegularExpressions;
using Nuclei.Features;

namespace Nuclei.CritzOS.Features;
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

// ReSharper disable once InconsistentNaming
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
    
    public static bool SendManualReport(string username, string message)
    {
        message = Regex.Replace(message, @"@", "");
        return SendDiscordMessage(username, $"<@&1489759287936024726> {message} \n\n" +
                                            $"*Mission: {MissionService.GetCurrentMission().Name}* \n" +
                                            $"*Server: {CritzOSGlobals.ServerName}*", CritzOSGlobals.ReportsChannelWebhookURL);
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
