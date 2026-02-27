using System;
using System.Collections.Specialized;
using System.Net;

namespace Nuclei.Features;
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

public class ReportCommandService
{


    private static string webhookURL = Environment.GetEnvironmentVariable("webhookURL")!;
    private static string chatLogWebhookURL = Environment.GetEnvironmentVariable("chatLogWebhookURL")!;
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